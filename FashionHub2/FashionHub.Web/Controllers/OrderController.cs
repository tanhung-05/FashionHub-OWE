using System.Security.Claims;
using System.Text.Json;
using FashionHub.Web.Data;
using FashionHub.Web.Models.Generated;
using FashionHub.Web.ViewModels.Cart;
using FashionHub.Web.ViewModels.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Controllers;

[Authorize]
public class OrderController : Controller
{
    private const string CartSessionKey = "CartSession";
    private const string BuyNowCartSessionKey = "BuyNowCart";
    private readonly ApplicationDbContext dbContext;

    public OrderController(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Order") });
        }

        // Lấy giỏ hàng
        List<CartItemViewModel> cartToCheckout;
        string cartType;

        var buyNowCartJson = HttpContext.Session.GetString(BuyNowCartSessionKey);
        if (!string.IsNullOrWhiteSpace(buyNowCartJson))
        {
            cartToCheckout = JsonSerializer.Deserialize<List<CartItemViewModel>>(buyNowCartJson) ?? new();
            cartType = "BuyNow";
        }
        else
        {
            var cartJson = HttpContext.Session.GetString(CartSessionKey);
            cartToCheckout = string.IsNullOrWhiteSpace(cartJson)
                ? new()
                : JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson) ?? new();
            cartType = "Normal";
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
                LaMacDinh = address.LaMacDinh ?? false,
                FullAddress = $"{address.ChiTiet}, {address.PhuongXa}, {address.QuanHuyen}, {address.TinhThanh}"
            })
            .ToListAsync();

        // Lấy phương thức thanh toán
        var paymentMethods = await dbContext.PhuongThucThanhToans
            .AsNoTracking()
            .Select(method => new PaymentMethodViewModel
            {
                IdphuongThucThanhToan = method.IdphuongThucThanhToan,
                TenPhuongThuc = method.TenPhuongThuc ?? string.Empty
            })
            .ToListAsync();

        var subtotal = cartToCheckout.Sum(item => item.ThanhTien);
        const decimal shippingFee = 30000;

        var viewModel = new CheckoutViewModel
        {
            CartItems = cartToCheckout,
            UserAddresses = addresses,
            PaymentMethods = paymentMethods,
            Subtotal = subtotal,
            ShippingFee = shippingFee,
            Discount = 0,
            AppliedCouponCode = string.Empty
        };

        ViewBag.CartType = cartType;
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(int addressId, int paymentMethodId, string cartType)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
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
                var cartJson = HttpContext.Session.GetString(CartSessionKey);
                cart = string.IsNullOrWhiteSpace(cartJson)
                    ? new()
                    : JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson) ?? new();
            }

            if (!cart.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // Kiểm tra tồn kho
            foreach (var item in cart)
            {
                var variant = await dbContext.BienTheSanPhams
                    .FirstOrDefaultAsync(v => v.IdbienThe == item.IdbienThe);

                if (variant == null || variant.SoLuongTon < item.SoLuong)
                {
                    TempData["Error"] = $"Sản phẩm {item.TenSanPham} không đủ số lượng.";
                    await transaction.RollbackAsync();
                    return RedirectToAction("Checkout");
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
            var address = await dbContext.DiaChis.FindAsync(addressId);
            if (address == null)
            {
                TempData["Error"] = "Địa chỉ không hợp lệ.";
                await transaction.RollbackAsync();
                return RedirectToAction("Checkout");
            }

            // Tạo đơn hàng
            var totalAmount = cart.Sum(item => item.ThanhTien);
            const decimal shippingFee = 40000;

            var order = new DonHang
            {
                IdnguoiDung = userId,
                TenNguoiNhan = address.TenNguoiNhan ?? string.Empty,
                DiaChiGiao = $"{address.ChiTiet}, {address.PhuongXa}, {address.QuanHuyen}, {address.TinhThanh}",
                SoDienThoai = address.SoDienThoai ?? string.Empty,
                TongTienHang = totalAmount,
                PhiVanChuyen = shippingFee,
                TienGiamGia = discount,
                TongThanhToan = totalAmount + shippingFee - discount,
                IdmaGiamGia = couponId,
                IdphuongThucThanhToan = paymentMethodId,
                IdtrangThai = 0,
                NgayTao = DateTime.Now
            };

            dbContext.DonHangs.Add(order);
            await dbContext.SaveChangesAsync();

            // Cập nhật coupon
            if (couponId.HasValue)
            {
                var coupon = await dbContext.MaGiamGia.FindAsync(couponId.Value);
                if (coupon != null)
                {
                    coupon.DaSuDung++;
                }
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
                    variant.SoLuongTon -= item.SoLuong;
                }
            }

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // Xóa session
            if (cartType == "BuyNow")
            {
                HttpContext.Session.Remove(BuyNowCartSessionKey);
            }
            else
            {
                HttpContext.Session.Remove(CartSessionKey);
            }

            HttpContext.Session.Remove("DiscountAmount");
            HttpContext.Session.Remove("CouponId");
            HttpContext.Session.Remove("CouponCode");

            return RedirectToAction("OrderSuccess", new { id = order.IddonHang });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Đã xảy ra lỗi trong quá trình đặt hàng. Vui lòng thử lại.";
            return RedirectToAction("Checkout");
        }
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
                var cartJson = HttpContext.Session.GetString(CartSessionKey);
                cart = string.IsNullOrWhiteSpace(cartJson)
                    ? new()
                    : JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson) ?? new();
            }

            if (!cart.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng trống, không thể áp dụng." });
            }

            var totalOrder = cart.Sum(item => item.ThanhTien);

            var coupon = await dbContext.MaGiamGia
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.MaCode == cleanCode && c.TrangThai == true);

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
            decimal discountAmount = 0;
            if (coupon.LoaiGiamGia == 1)
            {
                discountAmount = coupon.GiaTri;
            }
            else
            {
                discountAmount = totalOrder * (coupon.GiaTri / 100);
                if (coupon.GiamToiDa.HasValue && discountAmount > coupon.GiamToiDa.Value)
                {
                    discountAmount = coupon.GiamToiDa.Value;
                }
            }

            // Lưu session
            HttpContext.Session.SetString("CouponCode", coupon.MaCode ?? string.Empty);
            HttpContext.Session.SetString("DiscountAmount", discountAmount.ToString());
            HttpContext.Session.SetString("CouponId", coupon.IdmaGiamGia.ToString());

            const decimal shipping = 30000;
            var finalTotal = totalOrder + shipping - discountAmount;

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
}