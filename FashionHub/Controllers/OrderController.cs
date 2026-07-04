using FashionHub.Models;
using FashionHub.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace FashionHub.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private QL_SHOPQUANAO_PROEntities db = new QL_SHOPQUANAO_PROEntities();
        private const string CartSession = "CartSession";

        // GET: Checkout
        // GET: Checkout
        public ActionResult Checkout()
        {
            // 1. KIỂM TRA ĐĂNG NHẬP
            var user = Session["User"] as NguoiDung;
            if (user == null)
            {
                // Nếu chưa đăng nhập, chuyển hướng sang trang Login
                // Kèm theo returnUrl để sau khi đăng nhập xong tự động quay lại trang Checkout
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Order") });
            }

            // 2. Xử lý giỏ hàng
            List<CartItemViewModel> cartToCheckout;
            string cartType = "Normal";

            if (Session["BuyNowCart"] != null)
            {
                cartToCheckout = Session["BuyNowCart"] as List<CartItemViewModel>;
                cartType = "BuyNow";
            }
            else
            {
                cartToCheckout = Session[CartSession] as List<CartItemViewModel>;
            }

            // Nếu giỏ hàng trống thì đá về trang giỏ hàng
            if (cartToCheckout == null || !cartToCheckout.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // 3. Lấy danh sách địa chỉ (Lúc này user đã chắc chắn khác null)
            var userAddressesFromDb = db.DiaChis.Where(a => a.IDNguoiDung == user.IDNguoiDung).ToList();

            var addressViewModels = userAddressesFromDb.Select(addr => new AddressViewModel
            {
                IDDiaChi = addr.IDDiaChi,
                TenNguoiNhan = addr.TenNguoiNhan,
                SoDienThoai = addr.SoDienThoai,
                LaMacDinh = addr.LaMacDinh ?? false,
                // Ghép chuỗi từ các cột string mới (TinhThanh1, QuanHuyen1...)
                FullAddress = $"{addr.ChiTiet}, {addr.PhuongXa1}, {addr.QuanHuyen1}, {addr.TinhThanh1}"
            }).ToList();

            // 4. Tính toán tiền nong
            decimal subtotal = cartToCheckout.Sum(i => i.ThanhTien);
            decimal shippingFee = 30000;

            var viewModel = new CheckoutViewModel
            {
                CartItems = cartToCheckout,
                UserAddresses = addressViewModels,
                PaymentMethods = db.PhuongThucThanhToans.ToList(),
                Subtotal = subtotal,
                ShippingFee = shippingFee,
                Discount = 0,
                AppliedCouponCode = ""
            };

            ViewBag.CartType = cartType;
            return View(viewModel);
        }

        // POST: PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(int addressId, int paymentMethodId, string cartType)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. Lấy giỏ hàng
                    List<CartItemViewModel> cart;
                    if (cartType == "BuyNow")
                    {
                        cart = Session["BuyNowCart"] as List<CartItemViewModel>;
                    }
                    else
                    {
                        cart = Session[CartSession] as List<CartItemViewModel>;
                    }

                    if (cart == null || !cart.Any()) return RedirectToAction("Index", "Cart");

                    var user = Session["User"] as NguoiDung;

                    // 2. Kiểm tra tồn kho lần cuối
                    foreach (var item in cart)
                    {
                        var variant = db.BienTheSanPhams.Find(item.IDBienThe);
                        if (variant.SoLuongTon < item.SoLuong)
                        {
                            TempData["Error"] = $"Sản phẩm {item.TenSanPham} không đủ số lượng.";
                            transaction.Rollback();
                            return RedirectToAction("Checkout");
                        }
                    }
                    decimal discount = 0;
                    int? couponId = null;

                    if (Session["DiscountAmount"] != null)
                    {
                        discount = (decimal)Session["DiscountAmount"];
                        couponId = (int?)Session["CouponId"];
                    }

                    // 3. Tạo đơn hàng
                    var address = db.DiaChis.Find(addressId);
                    decimal tongTienHang = cart.Sum(i => i.ThanhTien);
                    var phiVanChuyen = 40000; 
                    var order = new DonHang
                    {
                        IDNguoiDung = user.IDNguoiDung,
                        TenNguoiNhan = address.TenNguoiNhan,
                        DiaChiGiao = $"{address.ChiTiet}, {address.PhuongXa1}, {address.QuanHuyen1}, {address.TinhThanh1}",
                        SoDienThoai = address.SoDienThoai,

                        TongTienHang = tongTienHang,
                        PhiVanChuyen = phiVanChuyen,

                        TienGiamGia = discount,
                        TongThanhToan = tongTienHang + phiVanChuyen - discount,
                        IDMaGiamGia = couponId,

                        IDPhuongThucThanhToan = paymentMethodId,
                        IDTrangThai = 0, // 0: Chờ xác nhận

                        NgayTao = DateTime.Now
                    };

                    db.DonHangs.Add(order);
                    db.SaveChanges();

                    if (couponId.HasValue)
                    {
                        var coupon = db.MaGiamGias.Find(couponId.Value);
                        if (coupon != null)
                        {
                            coupon.DaSuDung++; // Tăng số lượt đã dùng
                        }
                    }


                    foreach (var item in cart)
                    {
                        var orderDetail = new ChiTietDonHang
                        {
                            IDDonHang = order.IDDonHang,
                            IDBienThe = item.IDBienThe,
                            SoLuong = item.SoLuong,
                            DonGia = item.DonGia,
                            TenSanPham = item.TenSanPham,
                            TenMau = item.TenMau,
                            TenKichThuoc = item.TenKichThuoc
                        };
                        db.ChiTietDonHangs.Add(orderDetail);

                        // Trừ tồn kho
                        var variant = db.BienTheSanPhams.Find(item.IDBienThe);
                        variant.SoLuongTon -= item.SoLuong;
                    }
                    db.SaveChanges();

                    transaction.Commit();

                    // 5. Xóa session giỏ hàng
                    if (cartType == "BuyNow") Session["BuyNowCart"] = null;
                    else Session[CartSession] = null;

                    // Xóa session discount nếu còn sót lại
                    Session["Discount"] = null;

                    return RedirectToAction("OrderSuccess", new { id = order.IDDonHang });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    // Log lỗi ra console hoặc file log
                    System.Diagnostics.Debug.WriteLine("LỖI KHI ĐẶT HÀNG: " + ex.ToString());
                    TempData["Error"] = "Đã xảy ra lỗi trong quá trình đặt hàng. Vui lòng thử lại.";
                    return RedirectToAction("Checkout");
                }
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ApplyCoupon(string couponCode, string cartType)
        {
            try
            {
                // 1. Kiểm tra đầu vào
                if (string.IsNullOrEmpty(couponCode))
                    return Json(new { success = false, message = "Vui lòng nhập mã." });

                // Chuẩn hóa mã: Cắt khoảng trắng, chuyển về chữ hoa để so sánh
                string cleanCode = couponCode.Trim().ToUpper();

                // 2. Lấy giỏ hàng
                List<CartItemViewModel> cart;
                if (cartType == "BuyNow") cart = Session["BuyNowCart"] as List<CartItemViewModel>;
                else cart = Session[CartSession] as List<CartItemViewModel>;

                if (cart == null || !cart.Any())
                    return Json(new { success = false, message = "Giỏ hàng trống, không thể áp dụng." });

                decimal totalOrder = cart.Sum(x => x.ThanhTien);

                var coupon = db.MaGiamGias
                    .FirstOrDefault(x => x.MaCode == cleanCode && x.TrangThai == true);

                // --- BẮT LỖI CHI TIẾT ---
                if (coupon == null)
                    return Json(new { success = false, message = $"Mã '{cleanCode}' không tồn tại hoặc đã bị khóa." });

                if (coupon.SoLuong <= coupon.DaSuDung)
                    return Json(new { success = false, message = "Mã này đã hết lượt sử dụng." });

                if (DateTime.Now < coupon.NgayBatDau)
                    return Json(new { success = false, message = "Chương trình khuyến mãi chưa bắt đầu." });

                if (DateTime.Now > coupon.NgayKetThuc)
                    return Json(new { success = false, message = "Mã giảm giá đã hết hạn." });

                if (totalOrder < coupon.DonHangToiThieu)
                    return Json(new { success = false, message = $"Đơn hàng cần tối thiểu {coupon.DonHangToiThieu:N0}đ để dùng mã này." });

                // --- TÍNH TOÁN ---
                decimal discountAmount = 0;
                if (coupon.LoaiGiamGia == 1) // Tiền mặt
                {
                    discountAmount = coupon.GiaTri;
                }
                else // Phần trăm
                {
                    discountAmount = totalOrder * (coupon.GiaTri / 100);
                    if (coupon.GiamToiDa.HasValue && discountAmount > coupon.GiamToiDa.Value)
                    {
                        discountAmount = coupon.GiamToiDa.Value;
                    }
                }

                // Lưu Session
                Session["CouponCode"] = coupon.MaCode;
                Session["DiscountAmount"] = discountAmount;
                Session["CouponId"] = coupon.IDMaGiamGia;

                decimal shipping = 30000;
                decimal finalTotal = totalOrder + shipping - discountAmount;

                return Json(new
                {
                    success = true,
                    message = $"Áp dụng thành công! Giảm {discountAmount:N0}đ",
                    discount = discountAmount.ToString("N0"),
                    newTotal = finalTotal.ToString("N0")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        public ActionResult OrderSuccess(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }
    }
}