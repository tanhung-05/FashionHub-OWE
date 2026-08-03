using System.Security.Claims;
using System.Text.Json;
using FashionHub.Web.Data;
using FashionHub.Web.Application.Orders;
using FashionHub.Web.Application.Payments;
using FashionHub.Web.Domain;
using FashionHub.Web.Models.Generated;
using FashionHub.Web.Services;
using FashionHub.Web.ViewModels.Cart;
using FashionHub.Web.ViewModels.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Controllers;

[Authorize]
public class OrderController : Controller
{
    private const string BuyNowCartSessionKey = "BuyNowCart";
    private readonly ApplicationDbContext dbContext;
    private readonly ICartService cartService;
    private readonly IOrderService orderService;
    private readonly IVnPayService vnPayService;
    private readonly ILogger<OrderController> logger;

    public OrderController(
        ApplicationDbContext dbContext,
        ICartService cartService,
        IOrderService orderService,
        IVnPayService vnPayService,
        ILogger<OrderController> logger)
    {
        this.dbContext = dbContext;
        this.cartService = cartService;
        this.orderService = orderService;
        this.vnPayService = vnPayService;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(
        string cartType = "Normal",
        string? validationError = null)
    {
        var normalizedCartType = string.Equals(
            cartType,
            "BuyNow",
            StringComparison.OrdinalIgnoreCase)
            ? "BuyNow"
            : "Normal";
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToAction(
                "Login",
                "Account",
                new
                {
                    returnUrl = Url.Action(
                        "Checkout",
                        "Order",
                        new { cartType = normalizedCartType })
                });
        }

        List<CartItemViewModel> cartToCheckout;
        if (normalizedCartType == "BuyNow")
        {
            var buyNowCartJson = HttpContext.Session.GetString(BuyNowCartSessionKey);
            cartToCheckout = string.IsNullOrWhiteSpace(buyNowCartJson)
                ? new()
                : JsonSerializer.Deserialize<List<CartItemViewModel>>(buyNowCartJson) ?? new();
        }
        else
        {
            HttpContext.Session.Remove(BuyNowCartSessionKey);
            var cartResult = await cartService.GetCartAsync();
            cartToCheckout = cartResult.Value?.ToViewModels() ?? new();
        }

        if (!cartToCheckout.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        // Lấy địa chỉ người dùng
        var addresses = await dbContext.DiaChis
            .AsNoTracking()
            .Where(address => address.IdnguoiDung == userId)
            .Select(address => new AddressViewModel
            {
                IddiaChi = address.IddiaChi,
                TenNguoiNhan = address.TenNguoiNhan ?? string.Empty,
                SoDienThoai = address.SoDienThoai ?? string.Empty,
                LaMacDinh = address.LaMacDinh,
                FullAddress = $"{address.ChiTiet}, {address.PhuongXa}, {address.QuanHuyen}, {address.TinhThanh}"
            })
            .ToListAsync();

        // Lấy phương thức thanh toán
        var paymentMethods = await dbContext.PhuongThucThanhToans
            .AsNoTracking()
            .Where(method =>
                method.TrangThai
                && (method.MaPhuongThuc != PaymentMethodCodes.VnPay
                    || vnPayService.IsConfigured))
            .Select(method => new PaymentMethodViewModel
            {
                IdphuongThucThanhToan = method.IdphuongThucThanhToan,
                MaPhuongThuc = method.MaPhuongThuc,
                TenPhuongThuc = method.TenPhuongThuc ?? string.Empty
            })
            .ToListAsync();

        var subtotal = cartToCheckout.Sum(item => item.ThanhTien);
        var viewModel = new CheckoutViewModel
        {
            CartItems = cartToCheckout,
            UserAddresses = addresses,
            PaymentMethods = paymentMethods,
            Subtotal = subtotal,
            ShippingFee = ShippingFees.Standard,
            Discount = 0,
            AppliedCouponCode = string.Empty
        };

        ViewBag.CartType = normalizedCartType;
        ViewData["CheckoutError"] = validationError switch
        {
            "address" => "Vui lòng chọn hoặc thêm địa chỉ giao hàng trước khi đặt hàng.",
            "payment" => "Vui lòng chọn phương thức thanh toán.",
            _ => null
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(int addressId, int paymentMethodId, string cartType)
    {
        var normalizedCartType = string.Equals(
            cartType,
            "BuyNow",
            StringComparison.OrdinalIgnoreCase)
            ? "BuyNow"
            : "Normal";
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (addressId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn hoặc thêm địa chỉ giao hàng trước khi đặt hàng.";
            return RedirectToAction("Checkout", new
            {
                cartType = normalizedCartType,
                validationError = "address"
            });
        }

        if (paymentMethodId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn phương thức thanh toán.";
            return RedirectToAction("Checkout", new
            {
                cartType = normalizedCartType,
                validationError = "payment"
            });
        }

        var selectedPaymentMethod = await dbContext.PhuongThucThanhToans
            .AsNoTracking()
            .FirstOrDefaultAsync(method =>
                method.IdphuongThucThanhToan == paymentMethodId
                && method.TrangThai);
        if (selectedPaymentMethod == null
            || (selectedPaymentMethod.MaPhuongThuc == PaymentMethodCodes.VnPay
                && !vnPayService.IsConfigured))
        {
            TempData["Error"] = "Phương thức thanh toán chưa sẵn sàng. Vui lòng chọn phương thức khác.";
            return RedirectToAction("Checkout", new
            {
                cartType = normalizedCartType,
                validationError = "payment"
            });
        }

        if (normalizedCartType == "Normal")
        {
            try
            {
                var result = await orderService.CreateOrderAsync(new CreateOrderRequest
                {
                    AddressId = addressId,
                    PaymentMethodId = paymentMethodId,
                    CouponCode = HttpContext.Session.GetString("CouponCode")
                });
                if (!result.IsSuccess)
                {
                    TempData["Error"] = result.Error!.Message;
                    return RedirectToAction("Checkout", new { cartType = normalizedCartType });
                }

                HttpContext.Session.Remove("DiscountAmount");
                HttpContext.Session.Remove("CouponId");
                HttpContext.Session.Remove("CouponCode");
                if (result.Value!.PaymentMethodCode == PaymentMethodCodes.VnPay)
                {
                    var paymentResult = await vnPayService.CreatePaymentUrlAsync(
                        result.Value.Id,
                        userId.Value,
                        GetClientIpAddress());
                    if (paymentResult.IsSuccess)
                    {
                        return Redirect(paymentResult.Value!);
                    }

                    TempData["Error"] = paymentResult.Error!.Message;
                    return RedirectToAction(
                        "OrderDetail",
                        "Account",
                        new { id = result.Value.Id });
                }

                return RedirectToAction("OrderSuccess", new { id = result.Value.Id });
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to place normal cart order for user {UserId}",
                    userId);
                TempData["Error"] =
                    "Không thể đặt hàng lúc này. Giỏ hàng của bạn vẫn được giữ nguyên, vui lòng thử lại.";
                return RedirectToAction("Checkout", new { cartType = normalizedCartType });
            }
        }

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync<IActionResult>(async () =>
        {
            dbContext.ChangeTracker.Clear();
            await using var transaction =
                await dbContext.Database.BeginTransactionAsync();

            try
            {
                var buyNowJson = HttpContext.Session.GetString(BuyNowCartSessionKey);
                var cart = string.IsNullOrWhiteSpace(buyNowJson)
                    ? new List<CartItemViewModel>()
                    : JsonSerializer.Deserialize<List<CartItemViewModel>>(buyNowJson) ?? new();

                if (!cart.Any())
                {
                    return RedirectToAction("Index", "Cart");
                }

                // Kiểm tra tồn kho
                foreach (var item in cart)
                {
                    var variant = await dbContext.BienTheSanPhams
                        .Include(v => v.IdsanPhamNavigation)
                        .FirstOrDefaultAsync(v =>
                            v.IdbienThe == item.IdbienThe
                            && v.TrangThai
                            && v.DeletedAt == null
                            && v.IdsanPhamNavigation.TrangThai
                            && v.IdsanPhamNavigation.DeletedAt == null);

                    if (variant == null || variant.SoLuongTon < item.SoLuong)
                    {
                        TempData["Error"] = $"Sản phẩm {item.TenSanPham} không đủ số lượng.";
                        await transaction.RollbackAsync();
                        return RedirectToAction("Checkout", new { cartType = normalizedCartType });
                    }
                }

                // Lấy thông tin giảm giá
                decimal discount = 0;
                int? couponId = null;
                var discountSession = HttpContext.Session.GetString("DiscountAmount");
                var couponIdSession = HttpContext.Session.GetString("CouponId");

                if (!string.IsNullOrWhiteSpace(discountSession))
                {
                    decimal.TryParse(discountSession, out discount);
                }

                if (!string.IsNullOrWhiteSpace(couponIdSession))
                {
                    int.TryParse(couponIdSession, out var tempCouponId);
                    couponId = tempCouponId;
                }

                // Lấy địa chỉ
                var address = await dbContext.DiaChis
                    .FirstOrDefaultAsync(item =>
                        item.IddiaChi == addressId
                        && item.IdnguoiDung == userId);
                if (address == null)
                {
                    TempData["Error"] = "Địa chỉ không hợp lệ.";
                    await transaction.RollbackAsync();
                    return RedirectToAction("Checkout", new { cartType = normalizedCartType });
                }

                var paymentMethodIsActive = await dbContext.PhuongThucThanhToans
                    .AnyAsync(method =>
                        method.IdphuongThucThanhToan == paymentMethodId
                        && method.TrangThai);
                if (!paymentMethodIsActive)
                {
                    TempData["Error"] = "Phương thức thanh toán không hợp lệ.";
                    await transaction.RollbackAsync();
                    return RedirectToAction("Checkout", new { cartType = normalizedCartType });
                }

                var totalAmount = cart.Sum(item => item.ThanhTien);
                MaGiamGium? coupon = null;
                if (couponId.HasValue)
                {
                    coupon = await dbContext.MaGiamGia.FirstOrDefaultAsync(item =>
                        item.IdmaGiamGia == couponId.Value
                        && item.TrangThai
                        && item.DeletedAt == null
                        && item.DaSuDung < item.SoLuong
                        && item.NgayBatDau <= DateTime.Now
                        && item.NgayKetThuc >= DateTime.Now
                        && item.DonHangToiThieu <= totalAmount);

                    if (coupon == null)
                    {
                        couponId = null;
                        discount = 0;
                    }
                    else
                    {
                        discount = CalculateDiscount(coupon, totalAmount);
                    }
                }

                // Tạo đơn hàng
                var order = new DonHang
                {
                    IdnguoiDung = userId,
                    TenNguoiNhan = address.TenNguoiNhan ?? string.Empty,
                    DiaChiGiao = $"{address.ChiTiet}, {address.PhuongXa}, {address.QuanHuyen}, {address.TinhThanh}",
                    SoDienThoai = address.SoDienThoai ?? string.Empty,
                    TongTienHang = totalAmount,
                    PhiVanChuyen = ShippingFees.Standard,
                    TienGiamGia = discount,
                    TongThanhToan = totalAmount + ShippingFees.Standard - discount,
                    IdmaGiamGia = couponId,
                    IdphuongThucThanhToan = paymentMethodId,
                    TrangThaiThanhToan = selectedPaymentMethod.MaPhuongThuc == PaymentMethodCodes.VnPay
                        ? PaymentStatusIds.Pending
                        : PaymentStatusIds.Unpaid,
                    IdtrangThai = OrderStatusIds.Pending,
                    NgayTao = DateTime.Now
                };

                dbContext.DonHangs.Add(order);
                await dbContext.SaveChangesAsync();

                // Cập nhật coupon
                if (coupon != null)
                {
                    coupon.DaSuDung++;
                }

                // Tạo chi tiết đơn hàng và trừ tồn kho
                foreach (var item in cart)
                {
                    var orderDetail = new ChiTietDonHang
                    {
                        IddonHang = order.IddonHang,
                        IdbienThe = item.IdbienThe,
                        SoLuong = item.SoLuong,
                        DonGia = item.DonGia,
                        TenSanPham = item.TenSanPham,
                        TenMau = item.TenMau,
                        TenKichThuoc = item.TenKichThuoc
                    };
                    dbContext.ChiTietDonHangs.Add(orderDetail);

                    var variant = await dbContext.BienTheSanPhams.FindAsync(item.IdbienThe);
                    if (variant != null)
                    {
                        var previousStock = variant.SoLuongTon;
                        variant.SoLuongTon -= item.SoLuong;
                        variant.TongDaBan += item.SoLuong;
                        variant.NgayCapNhat = DateTime.Now;
                        dbContext.LichSuTonKhos.Add(new LichSuTonKho
                        {
                            IdbienThe = variant.IdbienThe,
                            IdnguoiThucHien = userId,
                            IddonHang = order.IddonHang,
                            LoaiThayDoi = InventoryChangeTypes.OrderPlaced,
                            SoLuongThayDoi = -item.SoLuong,
                            TonTruoc = previousStock,
                            TonSau = variant.SoLuongTon,
                            GhiChu = $"Xuất kho cho đơn hàng #{order.IddonHang}",
                            NgayTao = DateTime.Now
                        });
                    }
                }

                dbContext.LichSuDonHangs.Add(new LichSuDonHang
                {
                    IddonHang = order.IddonHang,
                    IdtrangThaiMoi = OrderStatusIds.Pending,
                    IdnguoiThucHien = userId,
                    GhiChu = "Khách hàng tạo đơn hàng",
                    NgayTao = DateTime.Now
                });

                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                HttpContext.Session.Remove(BuyNowCartSessionKey);

                HttpContext.Session.Remove("DiscountAmount");
                HttpContext.Session.Remove("CouponId");
                HttpContext.Session.Remove("CouponCode");

                if (selectedPaymentMethod.MaPhuongThuc == PaymentMethodCodes.VnPay)
                {
                    var paymentResult = await vnPayService.CreatePaymentUrlAsync(
                        order.IddonHang,
                        userId.Value,
                        GetClientIpAddress());
                    if (paymentResult.IsSuccess)
                    {
                        return Redirect(paymentResult.Value!);
                    }

                    TempData["Error"] = paymentResult.Error!.Message;
                    return RedirectToAction(
                        "OrderDetail",
                        "Account",
                        new { id = order.IddonHang });
                }

                return RedirectToAction("OrderSuccess", new { id = order.IddonHang });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Đã xảy ra lỗi trong quá trình đặt hàng. Vui lòng thử lại.";
                return RedirectToAction("Checkout", new { cartType = normalizedCartType });
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyCoupon(string couponCode, string cartType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã." });
            }

            var cleanCode = couponCode.Trim().ToUpper();

            // Lấy giỏ hàng
            List<CartItemViewModel> cart;
            if (cartType == "BuyNow")
            {
                var buyNowJson = HttpContext.Session.GetString(BuyNowCartSessionKey);
                cart = string.IsNullOrWhiteSpace(buyNowJson)
                    ? new()
                    : JsonSerializer.Deserialize<List<CartItemViewModel>>(buyNowJson) ?? new();
            }
            else
            {
                var cartResult = await cartService.GetCartAsync();
                cart = cartResult.Value?.ToViewModels() ?? new();
            }

            if (!cart.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng trống, không thể áp dụng." });
            }

            var totalOrder = cart.Sum(item => item.ThanhTien);

            var coupon = await dbContext.MaGiamGia
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.MaCode == cleanCode
                    && c.TrangThai == true
                    && c.DeletedAt == null);

            if (coupon == null)
            {
                return Json(new { success = false, message = $"Mã '{cleanCode}' không tồn tại hoặc đã bị khóa." });
            }

            if (coupon.SoLuong <= coupon.DaSuDung)
            {
                return Json(new { success = false, message = "Mã này đã hết lượt sử dụng." });
            }

            if (DateTime.Now < coupon.NgayBatDau)
            {
                return Json(new { success = false, message = "Chương trình khuyến mãi chưa bắt đầu." });
            }

            if (DateTime.Now > coupon.NgayKetThuc)
            {
                return Json(new { success = false, message = "Mã giảm giá đã hết hạn." });
            }

            if (totalOrder < coupon.DonHangToiThieu)
            {
                return Json(new { success = false, message = $"Đơn hàng cần tối thiểu {coupon.DonHangToiThieu:N0}đ để dùng mã này." });
            }

            // Tính giảm giá
            var discountAmount = CalculateDiscount(coupon, totalOrder);

            // Lưu session
            HttpContext.Session.SetString("CouponCode", coupon.MaCode ?? string.Empty);
            HttpContext.Session.SetString("DiscountAmount", discountAmount.ToString());
            HttpContext.Session.SetString("CouponId", coupon.IdmaGiamGia.ToString());

            var finalTotal = totalOrder + ShippingFees.Standard - discountAmount;

            return Json(new
            {
                success = true,
                message = $"Áp dụng thành công! Giảm {discountAmount:N0}đ",
                discount = discountAmount.ToString("N0"),
                newTotal = finalTotal.ToString("N0")
            });
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "Lỗi hệ thống." });
        }
    }

    [HttpGet]
    public IActionResult OrderSuccess(int id)
    {
        ViewBag.OrderId = id;
        return View();
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string GetClientIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

    private static decimal CalculateDiscount(MaGiamGium coupon, decimal orderTotal)
    {
        if (coupon.LoaiGiamGia == CouponTypes.FixedAmount)
        {
            return Math.Min(coupon.GiaTri, orderTotal);
        }

        var discount = orderTotal * (coupon.GiaTri / 100);
        if (coupon.GiamToiDa.HasValue)
        {
            discount = Math.Min(discount, coupon.GiamToiDa.Value);
        }

        return Math.Min(discount, orderTotal);
    }
}
