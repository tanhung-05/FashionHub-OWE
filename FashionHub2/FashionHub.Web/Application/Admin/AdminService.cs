using System.Text.Json;
using FashionHub.Web.Application.Authentication;
using FashionHub.Web.Application.Common;
using FashionHub.Web.Application.Orders;
using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using FashionHub.Web.Models.Generated;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Application.Admin;

public sealed class AdminService :
    IAdminProductService,
    IAdminOrderService,
    IAdminReportService
{
    private readonly ApplicationDbContext dbContext;
    private readonly ICurrentUserService currentUser;
    private readonly ILogger<AdminService> logger;

    public AdminService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        ILogger<AdminService> logger)
    {
        this.dbContext = dbContext;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<ServiceResult<PagedResult<AdminProductDto>>> GetProductsAsync(
        AdminProductQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var products = dbContext.SanPhams.AsNoTracking().AsQueryable();
        if (!query.IncludeDeleted)
        {
            products = products.Where(product => product.DeletedAt == null);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            products = products.Where(product => product.TenSanPham.Contains(search));
        }

        var totalItems = await products.CountAsync(cancellationToken);
        var entities = await products
            .Include(product => product.IddanhMucNavigation)
            .Include(product => product.IdthuongHieuNavigation)
            .Include(product => product.BienTheSanPhams)
            .OrderByDescending(product => product.NgayTao)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return ServiceResult<PagedResult<AdminProductDto>>.Success(
            new PagedResult<AdminProductDto>(
                entities.Select(MapProduct).ToList(),
                query.PageNumber,
                query.PageSize,
                totalItems));
    }

    public async Task<ServiceResult<AdminProductDto>> GetProductAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var product = await LoadProductAsync(productId, cancellationToken);
        return product == null
            ? NotFound<AdminProductDto>("product-not-found", "Không tìm thấy sản phẩm.")
            : ServiceResult<AdminProductDto>.Success(MapProduct(product));
    }

    public async Task<ServiceResult<AdminProductDto>> CreateProductAsync(
        SaveAdminProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateProductRequestAsync(request, null, cancellationToken);
        if (validation != null)
        {
            return validation;
        }

        var now = DateTime.Now;
        var product = new SanPham
        {
            TenSanPham = request.Name.Trim(),
            Slug = NormalizeOptional(request.Slug),
            MoTa = NormalizeOptional(request.Description),
            Gia = request.Price,
            GiaKhuyenMai = request.SalePrice,
            NgayBatDauKm = request.SaleStart,
            NgayKetThucKm = request.SaleEnd,
            IddanhMuc = request.CategoryId,
            IdthuongHieu = request.BrandId,
            TrangThai = request.IsActive,
            NgayTao = now
        };
        dbContext.SanPhams.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        AddAudit("CREATE", "SanPham", product.IdsanPham, null, request);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<AdminProductDto>.Success(
            MapProduct((await LoadProductAsync(product.IdsanPham, cancellationToken))!));
    }

    public async Task<ServiceResult<AdminProductDto>> UpdateProductAsync(
        int productId,
        SaveAdminProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await LoadProductAsync(productId, cancellationToken);
        if (product == null)
        {
            return NotFound<AdminProductDto>("product-not-found", "Không tìm thấy sản phẩm.");
        }

        var validation = await ValidateProductRequestAsync(
            request,
            productId,
            cancellationToken);
        if (validation != null)
        {
            return validation;
        }

        var oldData = MapProduct(product);
        product.TenSanPham = request.Name.Trim();
        product.Slug = NormalizeOptional(request.Slug);
        product.MoTa = NormalizeOptional(request.Description);
        product.Gia = request.Price;
        product.GiaKhuyenMai = request.SalePrice;
        product.NgayBatDauKm = request.SaleStart;
        product.NgayKetThucKm = request.SaleEnd;
        product.IddanhMuc = request.CategoryId;
        product.IdthuongHieu = request.BrandId;
        product.TrangThai = request.IsActive;
        product.NgayCapNhat = DateTime.Now;
        AddAudit("UPDATE", "SanPham", productId, oldData, request);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<AdminProductDto>.Success(
            MapProduct((await LoadProductAsync(productId, cancellationToken))!));
    }

    public async Task<ServiceResult<bool>> DeleteProductAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var product = await dbContext.SanPhams
            .FirstOrDefaultAsync(item => item.IdsanPham == productId, cancellationToken);
        if (product == null)
        {
            return NotFound<bool>("product-not-found", "Không tìm thấy sản phẩm.");
        }

        if (product.DeletedAt != null)
        {
            return ServiceResult<bool>.Failure(
                ServiceErrorType.Conflict,
                "product-already-deleted",
                "Sản phẩm đã được xóa trước đó.");
        }

        product.DeletedAt = DateTime.Now;
        product.TrangThai = false;
        product.NgayCapNhat = DateTime.Now;
        AddAudit("SOFT_DELETE", "SanPham", productId, null, null);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<PagedResult<AdminOrderSummaryDto>>> GetOrdersAsync(
        AdminOrderQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        if (query.FromDate.HasValue
            && query.ToDate.HasValue
            && query.FromDate.Value.Date > query.ToDate.Value.Date)
        {
            return ServiceResult<PagedResult<AdminOrderSummaryDto>>.Failure(
                ServiceErrorType.Validation,
                "invalid-date-range",
                "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");
        }

        var orders = dbContext.DonHangs.AsNoTracking().AsQueryable();
        if (query.StatusId.HasValue)
        {
            orders = orders.Where(order => order.IdtrangThai == query.StatusId.Value);
        }

        if (query.FromDate.HasValue)
        {
            orders = orders.Where(order => order.NgayTao >= query.FromDate.Value.Date);
        }

        if (query.ToDate.HasValue)
        {
            var exclusiveEnd = query.ToDate.Value.Date.AddDays(1);
            orders = orders.Where(order => order.NgayTao < exclusiveEnd);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (int.TryParse(search, out var orderId))
            {
                orders = orders.Where(order => order.IddonHang == orderId);
            }
            else
            {
                orders = orders.Where(order =>
                    order.TenNguoiNhan.Contains(search)
                    || (order.IdnguoiDungNavigation != null
                        && order.IdnguoiDungNavigation.Email.Contains(search)));
            }
        }

        var totalItems = await orders.CountAsync(cancellationToken);
        var items = await orders
            .OrderByDescending(order => order.NgayTao)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(order => new AdminOrderSummaryDto(
                order.IddonHang,
                order.IdnguoiDungNavigation == null
                    ? null
                    : order.IdnguoiDungNavigation.Email,
                order.TenNguoiNhan,
                order.NgayTao,
                order.TongThanhToan,
                order.IdtrangThai,
                order.IdtrangThaiNavigation.TenTrangThai,
                order.ChiTietDonHangs.Sum(item => item.SoLuong)))
            .ToListAsync(cancellationToken);

        return ServiceResult<PagedResult<AdminOrderSummaryDto>>.Success(
            new PagedResult<AdminOrderSummaryDto>(
                items,
                query.PageNumber,
                query.PageSize,
                totalItems));
    }

    public async Task<ServiceResult<AdminOrderDetailDto>> GetOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await LoadAdminOrderAsync(orderId, cancellationToken);
        return order == null
            ? NotFound<AdminOrderDetailDto>("order-not-found", "Không tìm thấy đơn hàng.")
            : ServiceResult<AdminOrderDetailDto>.Success(MapAdminOrder(order));
    }

    public async Task<ServiceResult<AdminOrderDetailDto>> UpdateStatusAsync(
        int orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.DonHangs
            .Include(item => item.ChiTietDonHangs)
            .FirstOrDefaultAsync(item => item.IddonHang == orderId, cancellationToken);
        if (order == null)
        {
            return NotFound<AdminOrderDetailDto>("order-not-found", "Không tìm thấy đơn hàng.");
        }

        if (!IsAllowedTransition(order.IdtrangThai, request.StatusId))
        {
            return ServiceResult<AdminOrderDetailDto>.Failure(
                ServiceErrorType.Conflict,
                "invalid-order-status-transition",
                "Không thể chuyển đơn hàng sang trạng thái này.");
        }

        var statusExists = await dbContext.TrangThaiDonHangs
            .AnyAsync(status => status.IdtrangThai == request.StatusId, cancellationToken);
        if (!statusExists)
        {
            return NotFound<AdminOrderDetailDto>(
                "order-status-not-found",
                "Trạng thái đơn hàng không tồn tại.");
        }

        var oldStatus = order.IdtrangThai;
        if (request.StatusId == OrderStatusIds.Cancelled)
        {
            foreach (var item in order.ChiTietDonHangs.Where(item => item.IdbienThe.HasValue))
            {
                var variant = await dbContext.BienTheSanPhams.FindAsync(
                    new object[] { item.IdbienThe!.Value },
                    cancellationToken);
                if (variant == null)
                {
                    continue;
                }

                var oldStock = variant.SoLuongTon;
                variant.SoLuongTon += item.SoLuong;
                variant.TongDaBan = Math.Max(0, variant.TongDaBan - item.SoLuong);
                dbContext.LichSuTonKhos.Add(new LichSuTonKho
                {
                    IdbienThe = variant.IdbienThe,
                    IdnguoiThucHien = currentUser.UserId,
                    IddonHang = orderId,
                    LoaiThayDoi = InventoryChangeTypes.OrderCancelled,
                    SoLuongThayDoi = item.SoLuong,
                    TonTruoc = oldStock,
                    TonSau = variant.SoLuongTon,
                    GhiChu = $"Admin hủy đơn #{orderId}",
                    NgayTao = DateTime.Now
                });
            }
        }

        order.IdtrangThai = request.StatusId;
        order.NgayCapNhat = DateTime.Now;
        dbContext.LichSuDonHangs.Add(new LichSuDonHang
        {
            IddonHang = orderId,
            IdtrangThaiCu = oldStatus,
            IdtrangThaiMoi = request.StatusId,
            IdnguoiThucHien = currentUser.UserId,
            GhiChu = NormalizeOptional(request.Note) ?? "Admin cập nhật trạng thái",
            NgayTao = DateTime.Now
        });
        AddAudit("UPDATE_STATUS", "DonHang", orderId, oldStatus, request.StatusId);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Admin {AdminId} changed order {OrderId} status from {OldStatus} to {NewStatus}",
            currentUser.UserId,
            orderId,
            oldStatus,
            request.StatusId);

        return ServiceResult<AdminOrderDetailDto>.Success(
            MapAdminOrder((await LoadAdminOrderAsync(orderId, cancellationToken))!));
    }

    public async Task<ServiceResult<AdminDashboardReportDto>> GetDashboardAsync(
        AdminReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var fromDate = (query.FromDate ?? DateTime.Today.AddDays(-30)).Date;
        var toDate = (query.ToDate ?? DateTime.Today).Date;
        if (fromDate > toDate)
        {
            return ServiceResult<AdminDashboardReportDto>.Failure(
                ServiceErrorType.Validation,
                "invalid-date-range",
                "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");
        }

        var exclusiveEnd = toDate.AddDays(1);
        var orders = dbContext.DonHangs
            .AsNoTracking()
            .Where(order => order.NgayTao >= fromDate && order.NgayTao < exclusiveEnd);
        var nonCancelled = orders.Where(order => order.IdtrangThai != OrderStatusIds.Cancelled);

        var result = new AdminDashboardReportDto(
            fromDate,
            toDate,
            await orders.CountAsync(cancellationToken),
            await orders.CountAsync(
                order => order.IdtrangThai == OrderStatusIds.Pending,
                cancellationToken),
            await orders.CountAsync(
                order => order.IdtrangThai == OrderStatusIds.Completed,
                cancellationToken),
            await nonCancelled.SumAsync(
                order => (decimal?)order.TongThanhToan,
                cancellationToken) ?? 0,
            await nonCancelled
                .SelectMany(order => order.ChiTietDonHangs)
                .SumAsync(item => (int?)item.SoLuong, cancellationToken) ?? 0,
            await dbContext.BienTheSanPhams.CountAsync(
                variant =>
                    variant.TrangThai
                    && variant.DeletedAt == null
                    && variant.SoLuongTon <= variant.SoLuongCanhBao,
                cancellationToken));
        return ServiceResult<AdminDashboardReportDto>.Success(result);
    }

    private async Task<ServiceResult<AdminProductDto>?> ValidateProductRequestAsync(
        SaveAdminProductRequest request,
        int? currentProductId,
        CancellationToken cancellationToken)
    {
        if (request.SalePrice.HasValue && request.SalePrice.Value > request.Price)
        {
            return ServiceResult<AdminProductDto>.Failure(
                ServiceErrorType.Validation,
                "invalid-sale-price",
                "Giá khuyến mãi không được lớn hơn giá gốc.");
        }

        if (request.SaleStart.HasValue
            && request.SaleEnd.HasValue
            && request.SaleStart.Value >= request.SaleEnd.Value)
        {
            return ServiceResult<AdminProductDto>.Failure(
                ServiceErrorType.Validation,
                "invalid-sale-period",
                "Thời gian kết thúc khuyến mãi phải sau thời gian bắt đầu.");
        }

        if (request.CategoryId.HasValue
            && !await dbContext.DanhMucs.AnyAsync(category =>
                category.IddanhMuc == request.CategoryId.Value
                && category.DeletedAt == null,
                cancellationToken))
        {
            return NotFound<AdminProductDto>(
                "category-not-found",
                "Danh mục không tồn tại.");
        }

        if (request.BrandId.HasValue
            && !await dbContext.ThuongHieus.AnyAsync(brand =>
                brand.IdthuongHieu == request.BrandId.Value
                && brand.DeletedAt == null,
                cancellationToken))
        {
            return NotFound<AdminProductDto>(
                "brand-not-found",
                "Thương hiệu không tồn tại.");
        }

        var slug = NormalizeOptional(request.Slug);
        if (slug != null
            && await dbContext.SanPhams.AnyAsync(product =>
                product.Slug == slug
                && product.DeletedAt == null
                && (!currentProductId.HasValue
                    || product.IdsanPham != currentProductId.Value),
                cancellationToken))
        {
            return ServiceResult<AdminProductDto>.Failure(
                ServiceErrorType.Conflict,
                "product-slug-conflict",
                "Slug sản phẩm đã tồn tại.");
        }

        return null;
    }

    private Task<SanPham?> LoadProductAsync(
        int productId,
        CancellationToken cancellationToken) =>
        dbContext.SanPhams
            .Include(product => product.IddanhMucNavigation)
            .Include(product => product.IdthuongHieuNavigation)
            .Include(product => product.BienTheSanPhams)
            .FirstOrDefaultAsync(product => product.IdsanPham == productId, cancellationToken);

    private Task<DonHang?> LoadAdminOrderAsync(
        int orderId,
        CancellationToken cancellationToken) =>
        dbContext.DonHangs
            .AsNoTracking()
            .Include(order => order.IdnguoiDungNavigation)
            .Include(order => order.IdtrangThaiNavigation)
            .Include(order => order.IdphuongThucThanhToanNavigation)
            .Include(order => order.ChiTietDonHangs)
            .FirstOrDefaultAsync(order => order.IddonHang == orderId, cancellationToken);

    private static AdminProductDto MapProduct(SanPham product) =>
        new(
            product.IdsanPham,
            product.TenSanPham,
            product.Slug,
            product.MoTa,
            product.Gia,
            product.GiaKhuyenMai,
            product.NgayBatDauKm,
            product.NgayKetThucKm,
            product.IddanhMuc,
            product.IddanhMucNavigation?.TenDanhMuc,
            product.IdthuongHieu,
            product.IdthuongHieuNavigation?.TenThuongHieu,
            product.TrangThai,
            product.NgayTao,
            product.NgayCapNhat,
            product.DeletedAt,
            product.BienTheSanPhams.Count,
            product.BienTheSanPhams
                .Where(variant => variant.DeletedAt == null)
                .Sum(variant => variant.SoLuongTon));

    private static AdminOrderDetailDto MapAdminOrder(DonHang order) =>
        new(
            new OrderDetailDto(
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
                order.GhiChu,
                order.ChiTietDonHangs.Select(item => new OrderItemDto(
                    item.IdchiTietDonHang,
                    item.IdbienThe,
                    item.TenSanPham,
                    item.TenMau,
                    item.TenKichThuoc,
                    item.DonGia,
                    item.SoLuong)).ToList()),
            order.IdnguoiDung,
            order.IdnguoiDungNavigation?.Email);

    private void AddAudit(
        string action,
        string table,
        object id,
        object? oldData,
        object? newData)
    {
        dbContext.AdminActivityLogs.Add(new AdminActivityLog
        {
            Idadmin = currentUser.UserId,
            HanhDong = action,
            TenBang = table,
            IdbanGhi = id.ToString(),
            DuLieuCu = oldData == null ? null : JsonSerializer.Serialize(oldData),
            DuLieuMoi = newData == null ? null : JsonSerializer.Serialize(newData),
            NgayTao = DateTime.Now
        });
    }

    private static bool IsAllowedTransition(int currentStatus, int nextStatus) =>
        (currentStatus, nextStatus) switch
        {
            (OrderStatusIds.Pending, OrderStatusIds.Confirmed) => true,
            (OrderStatusIds.Pending, OrderStatusIds.Cancelled) => true,
            (OrderStatusIds.Confirmed, OrderStatusIds.Shipping) => true,
            (OrderStatusIds.Confirmed, OrderStatusIds.Cancelled) => true,
            (OrderStatusIds.Shipping, OrderStatusIds.Completed) => true,
            _ => false
        };

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ServiceResult<T> NotFound<T>(string code, string message) =>
        ServiceResult<T>.Failure(ServiceErrorType.NotFound, code, message);
}
