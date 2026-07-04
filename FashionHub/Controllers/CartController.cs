using FashionHub.Models;
using FashionHub.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace FashionHub.Controllers
{
    public class CartController : Controller
    {
        private QL_SHOPQUANAO_PROEntities db = new QL_SHOPQUANAO_PROEntities();
        private const string CartSession = "CartSession";

        private List<CartItemViewModel> GetCart()
        {
            var cart = Session[CartSession] as List<CartItemViewModel>;
            if (cart == null)
            {
                cart = new List<CartItemViewModel>();
                Session[CartSession] = cart;
            }
            return cart;
        }
        public ActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        [HttpGet]
        public JsonResult GetProductDetails(int productId)
        {
            var product = db.SanPhams
                .Where(p => p.IDSanPham == productId && p.TrangThai == true)
                .Select(p => new
                {
                    id = p.IDSanPham,
                    name = p.TenSanPham,
                    variants = p.BienTheSanPhams
                                .Where(v => v.SoLuongTon > 0)
                                .Select(v => new
                                {
                                    variantId = v.IDBienThe,
                                    colorId = v.IDMauSac,
                                    colorName = v.MauSac.TenMau,
                                    sizeId = v.IDKichThuoc,
                                    sizeName = v.KichThuoc.TenKichThuoc,
                                    price = p.Gia,
                                    stock = v.SoLuongTon,
                                    imageIds = v.HinhAnh_BienThe.Select(ha => ha.IDHinhAnh).ToList()
                                }).ToList(),
                    images = p.BienTheSanPhams
                              .SelectMany(bt => bt.HinhAnh_BienThe)
                              .Select(ha => new { id = ha.IDHinhAnh, url = ha.HinhAnh.DuongDan })
                              .Distinct()
                              .ToList()
                })
                .FirstOrDefault();

            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = product }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult AddToCart(int variantId, int quantity)
        {
            var cart = GetCart();

            // 1. Lấy thông tin biến thể từ DB trước
            var variant = db.BienTheSanPhams
                .Include(v => v.SanPham)
                .Include(v => v.MauSac)
                .Include(v => v.KichThuoc)
                .FirstOrDefault(v => v.IDBienThe == variantId);

            // 2. Kiểm tra hợp lệ
            if (variant == null) return Json(new { success = false, message = "Sản phẩm không hợp lệ." });
            if (variant.SoLuongTon < quantity) return Json(new { success = false, message = "Số lượng tồn kho không đủ." });

            // 3. TÍNH TOÁN GIÁ (LOGIC KHUYẾN MÃI THEO NGÀY)
            decimal finalPrice = variant.SanPham.Gia; // Mặc định là giá gốc

            // Kiểm tra: Có giá KM + Có ngày bắt đầu/kết thúc + Hôm nay nằm trong khoảng đó
            if (variant.SanPham.GiaKhuyenMai.HasValue
                && variant.SanPham.NgayBatDauKM.HasValue
                && variant.SanPham.NgayKetThucKM.HasValue
                && DateTime.Now >= variant.SanPham.NgayBatDauKM.Value
                && DateTime.Now <= variant.SanPham.NgayKetThucKM.Value)
            {
                finalPrice = variant.SanPham.GiaKhuyenMai.Value;
            }

            // 4. Thêm vào giỏ hàng
            var cartItem = cart.FirstOrDefault(item => item.IDBienThe == variantId);
            if (cartItem != null)
            {
                cartItem.SoLuong += quantity;
                // Cập nhật lại giá mới nhất nếu lỡ giá thay đổi
                cartItem.DonGia = finalPrice;
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    IDBienThe = variant.IDBienThe,
                    TenSanPham = variant.SanPham.TenSanPham,
                    TenMau = variant.MauSac?.TenMau,
                    TenKichThuoc = variant.KichThuoc?.TenKichThuoc,
                    DonGia = finalPrice, // Sử dụng giá đã tính toán ở trên
                    SoLuong = quantity,
                    AnhDaiDien = db.HinhAnh_BienThe
                                    .Where(ha => ha.IDBienThe == variantId && ha.LaAnhChinh == true)
                                    .Select(ha => ha.HinhAnh.DuongDan)
                                    .FirstOrDefault() ?? "/Content/images/placeholder.png"
                });
            }

            Session[CartSession] = cart;
            return Json(new { success = true, message = "Thêm vào giỏ hàng thành công!", cartCount = cart.Count() });
        }

        [HttpPost]
        public JsonResult BuyNow(int variantId, int quantity)
        {
            // 1. Lấy thông tin biến thể
            var variant = db.BienTheSanPhams
                .Include(v => v.SanPham)
                .Include(v => v.MauSac)
                .Include(v => v.KichThuoc)
                .FirstOrDefault(v => v.IDBienThe == variantId);

            // 2. Kiểm tra hợp lệ
            if (variant == null) return Json(new { success = false, message = "Sản phẩm không hợp lệ." });
            if (variant.SoLuongTon < quantity) return Json(new { success = false, message = "Số lượng tồn kho không đủ." });

            // 3. TÍNH TOÁN GIÁ (LOGIC KHUYẾN MÃI THEO NGÀY)
            decimal finalPrice = variant.SanPham.Gia;

            if (variant.SanPham.GiaKhuyenMai.HasValue
                && variant.SanPham.NgayBatDauKM.HasValue
                && variant.SanPham.NgayKetThucKM.HasValue
                && DateTime.Now >= variant.SanPham.NgayBatDauKM.Value
                && DateTime.Now <= variant.SanPham.NgayKetThucKM.Value)
            {
                finalPrice = variant.SanPham.GiaKhuyenMai.Value;
            }

            // 4. Tạo giỏ hàng mua ngay
            var buyNowCart = new List<CartItemViewModel> {
        new CartItemViewModel {
            IDBienThe = variant.IDBienThe,
            TenSanPham = variant.SanPham.TenSanPham,
            TenMau = variant.MauSac?.TenMau,
            TenKichThuoc = variant.KichThuoc?.TenKichThuoc,
            DonGia = finalPrice, // Sử dụng giá đã tính toán
            SoLuong = quantity,
            AnhDaiDien = db.HinhAnh_BienThe
                            .Where(ha => ha.IDBienThe == variantId && ha.LaAnhChinh == true)
                            .Select(ha => ha.HinhAnh.DuongDan)
                            .FirstOrDefault() ?? "/Content/images/placeholder.png"
        }
    };

            Session["BuyNowCart"] = buyNowCart;
            return Json(new { success = true, redirectUrl = Url.Action("Checkout", "Order") });
        }
        [HttpGet]
        public PartialViewResult GetCartOffcanvas()
        {
            var cart = GetCart();
            return PartialView("_CartOffcanvasPartial", cart);
        }

        [ChildActionOnly]
        public PartialViewResult CartIcon()
        {
            var cart = GetCart();
            ViewBag.CartItemCount = cart.Count;
            return PartialView("_CartIconPartial");
        }

        [HttpGet]
        public JsonResult GetCartItemCount()
        {
            var cart = GetCart();
            return Json(new { success = true, count = cart.Count() }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult UpdateCart(int variantId, int quantity)
        {
            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(i => i.IDBienThe == variantId);
            if (cartItem != null)
            {
                var variant = db.BienTheSanPhams.Find(variantId);
                if (variant.SoLuongTon < quantity) return Json(new { success = false, message = $"Chỉ còn {variant.SoLuongTon} sản phẩm." });
                cartItem.SoLuong = quantity;
                Session[CartSession] = cart;
                return Json(new { success = true, itemTotal = cartItem.ThanhTien.ToString("N0"), cartTotal = cart.Sum(i => i.ThanhTien).ToString("N0"), cartCount = cart.Count() });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public JsonResult RemoveFromCart(int variantId)
        {
            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(i => i.IDBienThe == variantId);
            if (cartItem != null)
            {
                cart.Remove(cartItem);
                Session[CartSession] = cart;
                return Json(new { success = true, cartTotal = cart.Sum(i => i.ThanhTien).ToString("N0"), cartCount = cart.Count() });
            }
            return Json(new { success = false });
        }
    }
}