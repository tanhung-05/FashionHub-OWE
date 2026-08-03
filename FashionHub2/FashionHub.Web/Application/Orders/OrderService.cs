using FashionHub.Web.Application.Authentication;
using FashionHub.Web.Application.Cart;
using FashionHub.Web.Application.Common;
using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using FashionHub.Web.Models.Generated;
using FashionHub.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FashionHub.Web.Application.Orders;

public sealed class OrderService : IOrderService
{
    private readonly ApplicationDbContext dbContext;
    private readonly ICartService cartService;
    private readonly ICurrentUserService currentUser;
    private readonly ILogger<OrderService> logger;

    public OrderService(
        ApplicationDbContext dbContext,
        ICartService cartService,
        ICurrentUserService currentUser,
        ILogger<OrderService> logger)
    {
        this.dbContext = dbContext;
        this.cartService = cartService;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<ServiceResult<PagedResult<OrderSummaryDto>>> GetOrdersAsync(
        OrderQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.UserId.HasValue)
        {
            return ServiceResult<PagedResult<OrderSummaryDto>>.Failure(
                ServiceErrorType.Unauthorized,
                "authentication-required",
                "Vui lòng đăng nhập.");
        }

        var orders = dbContext.DonHangs
            .AsNoTracking()
            .Where(order => order.IdnguoiDung == currentUser.UserId.Value);

        if (query.StatusId.HasValue)
        {
            orders = orders.Where(order => order.IdtrangThai == query.StatusId.Value);
        }

        var totalItems = await orders.CountAsync(cancellationToken);
        var items = await orders
            .OrderByDescending(order => order.NgayTao)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(order => new OrderSummaryDto(
                order.IddonHang,
                order.NgayTao,
                order.TongThanhToan,
                order.IdtrangThai,
                order.IdtrangThaiNavigation.TenTrangThai,
                order.ChiTietDonHangs.Sum(item => item.SoLuong)))
            .ToListAsync(cancellationToken);

        return ServiceResult<PagedResult<OrderSummaryDto>>.Success(
            new PagedResult<OrderSummaryDto>(
                items,
                query.PageNumber,
                query.PageSize,
                totalItems));
    }

    public async Task<ServiceResult<OrderDetailDto>> GetOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        var order = await LoadOrderAsync(
            orderId,
            currentUser.UserId.Value,
            cancellationToken);
        return order == null
            ? ServiceResult<OrderDetailDto>.Failure(
                ServiceErrorType.NotFound,
                "order-not-found",
                "Không tìm thấy đơn hàng.")
            : ServiceResult<OrderDetailDto>.Success(MapOrder(order));
    }

    public async Task<ServiceResult<OrderDetailDto>> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        var userId = currentUser.UserId.Value;
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();

            var cartResult = await cartService.GetCartAsync(cancellationToken);
            if (!cartResult.IsSuccess)
            {
                return ServiceResult<OrderDetailDto>.Failure(
                    cartResult.Error!.Type,
                    cartResult.Error.Code,
                    cartResult.Error.Message);
            }

            if (cartResult.Value!.Items.Count == 0)
            {
                return Failure(
                    ServiceErrorType.Conflict,
                    "empty-cart",
                    "Giỏ hàng đang trống.");
            }

            var address = await dbContext.DiaChis
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.IddiaChi == request.AddressId
                    && item.IdnguoiDung == userId,
                    cancellationToken);
            if (address == null)
            {
                return Failure(
                    ServiceErrorType.NotFound,
                    "shipping-address-not-found",
                    "Địa chỉ giao hàng không hợp lệ.");
            }

            var paymentMethod = await dbContext.PhuongThucThanhToans
                .AsNoTracking()
                .FirstOrDefaultAsync(method =>
                    method.IdphuongThucThanhToan == request.PaymentMethodId
                    && method.TrangThai,
                    cancellationToken);
            if (paymentMethod == null)
            {
                return Failure(
                    ServiceErrorType.NotFound,
                    "payment-method-not-found",
                    "Phương thức thanh toán không hợp lệ.");
            }

            var cartItems = cartResult.Value.Items;
            var variantIds = cartItems.Select(item => item.VariantId).ToList();
            var variants = await dbContext.BienTheSanPhams
                .Where(variant =>
                    variantIds.Contains(variant.IdbienThe)
                    && variant.TrangThai
                    && variant.DeletedAt == null
                    && variant.IdsanPhamNavigation.TrangThai
                    && variant.IdsanPhamNavigation.DeletedAt == null)
                .Include(variant => variant.IdsanPhamNavigation)
                .Include(variant => variant.IdmauSacNavigation)
                .Include(variant => variant.IdkichThuocNavigation)
                .ToDictionaryAsync(variant => variant.IdbienThe, cancellationToken);

            var orderLines = new List<ValidatedOrderLine>();
            foreach (var cartItem in cartItems)
            {
                if (!variants.TryGetValue(cartItem.VariantId, out var variant))
                {
                    return Failure(
                        ServiceErrorType.Conflict,
                        "cart-product-unavailable",
                        $"Sản phẩm '{cartItem.ProductName}' không còn khả dụng.");
                }

                if (variant.SoLuongTon < cartItem.Quantity)
                {
                    return Failure(
                        ServiceErrorType.Conflict,
                        "insufficient-stock",
                        $"Sản phẩm '{cartItem.ProductName}' chỉ còn {variant.SoLuongTon}.");
                }

                orderLines.Add(new ValidatedOrderLine(
                    variant,
                    cartItem.Quantity,
                    GetFinalPrice(variant)));
            }

            var subtotal = orderLines.Sum(item => item.UnitPrice * item.Quantity);
            var couponResult = await GetCouponAsync(
                request.CouponCode,
                subtotal,
                cancellationToken);
            if (!couponResult.IsSuccess)
            {
                return ServiceResult<OrderDetailDto>.Failure(
                    couponResult.Error!.Type,
                    couponResult.Error.Code,
                    couponResult.Error.Message);
            }

            var coupon = couponResult.Value;
            var discount = coupon == null ? 0 : CalculateDiscount(coupon, subtotal);
            IDbContextTransaction? transaction = null;

            try
            {
                if (dbContext.Database.IsRelational())
                {
                    transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                }

                var now = DateTime.Now;
                var order = new DonHang
                {
                    IdnguoiDung = userId,
                    IdmaGiamGia = coupon?.IdmaGiamGia,
                    TenNguoiNhan = address.TenNguoiNhan,
                    DiaChiGiao = FormatAddress(address),
                    SoDienThoai = address.SoDienThoai,
                    TongTienHang = subtotal,
                    PhiVanChuyen = ShippingFees.Standard,
                    TienGiamGia = discount,
                    TongThanhToan = subtotal + ShippingFees.Standard - discount,
                    IdphuongThucThanhToan = request.PaymentMethodId,
                    TrangThaiThanhToan = string.Equals(
                        paymentMethod.MaPhuongThuc,
                        PaymentMethodCodes.VnPay,
                        StringComparison.OrdinalIgnoreCase)
                        ? PaymentStatusIds.Pending
                        : PaymentStatusIds.Unpaid,
                    IdtrangThai = OrderStatusIds.Pending,
                    GhiChu = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                    NgayTao = now
                };
                dbContext.DonHangs.Add(order);
                await dbContext.SaveChangesAsync(cancellationToken);

                foreach (var line in orderLines)
                {
                    dbContext.ChiTietDonHangs.Add(new ChiTietDonHang
                    {
                        IddonHang = order.IddonHang,
                        IdbienThe = line.Variant.IdbienThe,
                        SoLuong = line.Quantity,
                        DonGia = line.UnitPrice,
                        TenSanPham = line.Variant.IdsanPhamNavigation.TenSanPham,
                        TenMau = line.Variant.IdmauSacNavigation?.TenMau,
                        TenKichThuoc = line.Variant.IdkichThuocNavigation?.TenKichThuoc
                    });

                    var previousStock = line.Variant.SoLuongTon;
                    line.Variant.SoLuongTon -= line.Quantity;
                    line.Variant.TongDaBan += line.Quantity;
                    line.Variant.NgayCapNhat = now;
                    dbContext.LichSuTonKhos.Add(new LichSuTonKho
                    {
                        IdbienThe = line.Variant.IdbienThe,
                        IdnguoiThucHien = userId,
                        IddonHang = order.IddonHang,
                        LoaiThayDoi = InventoryChangeTypes.OrderPlaced,
                        SoLuongThayDoi = -line.Quantity,
                        TonTruoc = previousStock,
                        TonSau = line.Variant.SoLuongTon,
                        GhiChu = $"Xuất kho cho đơn hàng #{order.IddonHang}",
                        NgayTao = now
                    });
                }

                if (coupon != null)
                {
                    coupon.DaSuDung++;
                }

                dbContext.LichSuDonHangs.Add(new LichSuDonHang
                {
                    IddonHang = order.IddonHang,
                    IdtrangThaiMoi = OrderStatusIds.Pending,
                    IdnguoiThucHien = userId,
                    GhiChu = "Khách hàng tạo đơn hàng",
                    NgayTao = now
                });

                var databaseCart = await dbContext.GioHangs
                    .Where(item => item.IdnguoiDung == userId)
                    .ToListAsync(cancellationToken);
                dbContext.GioHangs.RemoveRange(databaseCart);

                await dbContext.SaveChangesAsync(cancellationToken);
                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                logger.LogInformation(
                    "Created order {OrderId} for user {UserId}",
                    order.IddonHang,
                    userId);
                var createdOrder = await LoadOrderAsync(
                    order.IddonHang,
                    userId,
                    cancellationToken);
                return ServiceResult<OrderDetailDto>.Success(MapOrder(createdOrder!));
            }
            catch (Exception exception)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }

                logger.LogError(
                    exception,
                    "Failed to create order for user {UserId}",
                    userId);
                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        });
    }

    private async Task<ServiceResult<MaGiamGium?>> GetCouponAsync(
        string? couponCode,
        decimal subtotal,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return ServiceResult<MaGiamGium?>.Success(null);
        }

        var code = couponCode.Trim().ToUpperInvariant();
        var coupon = await dbContext.MaGiamGia.FirstOrDefaultAsync(item =>
            item.MaCode == code
            && item.TrangThai
            && item.DeletedAt == null,
            cancellationToken);
        var now = DateTime.Now;
        if (coupon == null
            || coupon.DaSuDung >= coupon.SoLuong
            || now < coupon.NgayBatDau
            || now > coupon.NgayKetThuc
            || subtotal < coupon.DonHangToiThieu)
        {
            return ServiceResult<MaGiamGium?>.Failure(
                ServiceErrorType.Conflict,
                "coupon-not-applicable",
                "Mã giảm giá không hợp lệ hoặc không áp dụng được cho đơn hàng này.");
        }

        return ServiceResult<MaGiamGium?>.Success(coupon);
    }

    private Task<DonHang?> LoadOrderAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken)
    {
        return dbContext.DonHangs
            .AsNoTracking()
            .Include(order => order.IdtrangThaiNavigation)
            .Include(order => order.IdphuongThucThanhToanNavigation)
            .Include(order => order.ChiTietDonHangs)
            .FirstOrDefaultAsync(order =>
                order.IddonHang == orderId
                && order.IdnguoiDung == userId,
                cancellationToken);
    }

    private static OrderDetailDto MapOrder(DonHang order)
    {
        return new OrderDetailDto(
            order.IddonHang,
            order.NgayTao,
            order.NgayCapNhat,
            new ShippingAddressDto(
                order.TenNguoiNhan,
                order.SoDienThoai,
                order.DiaChiGiao),
            order.TongTienHang,
            order.PhiVanChuyen,
            order.TienGiamGia,
            order.TongThanhToan,
            order.IdtrangThai,
            order.IdtrangThaiNavigation.TenTrangThai,
            order.IdphuongThucThanhToanNavigation?.TenPhuongThuc,
            order.IdphuongThucThanhToanNavigation?.MaPhuongThuc,
            order.TrangThaiThanhToan,
            order.NgayThanhToan,
            order.GhiChu,
            order.ChiTietDonHangs
                .OrderBy(item => item.IdchiTietDonHang)
                .Select(item => new OrderItemDto(
                    item.IdchiTietDonHang,
                    item.IdbienThe,
                    item.TenSanPham,
                    item.TenMau,
                    item.TenKichThuoc,
                    item.DonGia,
                    item.SoLuong))
                .ToList());
    }

    private static decimal GetFinalPrice(BienTheSanPham variant)
    {
        var product = variant.IdsanPhamNavigation;
        var now = DateTime.Now;
        if (product.GiaKhuyenMai.HasValue
            && product.NgayBatDauKm.HasValue
            && product.NgayKetThucKm.HasValue
            && now >= product.NgayBatDauKm.Value
            && now <= product.NgayKetThucKm.Value)
        {
            return product.GiaKhuyenMai.Value;
        }

        return variant.Gia > 0 ? variant.Gia : product.Gia;
    }

    private static decimal CalculateDiscount(MaGiamGium coupon, decimal subtotal)
    {
        var discount = coupon.LoaiGiamGia == 1
            ? coupon.GiaTri
            : subtotal * coupon.GiaTri / 100;
        return coupon.GiamToiDa.HasValue
            ? Math.Min(discount, coupon.GiamToiDa.Value)
            : discount;
    }

    private static string FormatAddress(DiaChi address) =>
        $"{address.ChiTiet}, {address.PhuongXa}, {address.QuanHuyen}, {address.TinhThanh}";

    private static ServiceResult<OrderDetailDto> Unauthorized() =>
        Failure(
            ServiceErrorType.Unauthorized,
            "authentication-required",
            "Vui lòng đăng nhập.");

    private static ServiceResult<OrderDetailDto> Failure(
        ServiceErrorType type,
        string code,
        string message) =>
        ServiceResult<OrderDetailDto>.Failure(type, code, message);

    private sealed record ValidatedOrderLine(
        BienTheSanPham Variant,
        int Quantity,
        decimal UnitPrice);
}
