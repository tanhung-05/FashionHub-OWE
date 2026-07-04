using System.Text.Json;
using FashionHub.Web.Data;
using FashionHub.Web.ViewModels.Cart;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Controllers;

public class CartController : Controller
{
    private const string CartSessionKey = "CartSession";
    private const string BuyNowCartSessionKey = "BuyNowCart";
    private readonly ApplicationDbContext dbContext;

    public CartController(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public IActionResult Index()
    {
        var cart = GetCart();
        return View(cart);
    }

    [HttpGet]
    public async Task<IActionResult> GetProductDetails(int productId)
    {
        var product = await dbContext.SanPhams
            .AsNoTracking()
            .Where(product => product.IdsanPham == productId && product.TrangThai == true)
            .Select(product => new
            {
                id = product.IdsanPham,
                name = product.TenSanPham,
                variants = product.BienTheSanPhams
                    .Where(variant => variant.SoLuongTon > 0)
                    .Select(variant => new
                    {
                        variantId = variant.IdbienThe,
                        colorId = variant.IdmauSac,
                        colorName = variant.IdmauSacNavigation != null ? variant.IdmauSacNavigation.TenMau : null,
                        sizeId = variant.IdkichThuoc,
                        sizeName = variant.IdkichThuocNavigation != null ? variant.IdkichThuocNavigation.TenKichThuoc : null,
                        price = product.Gia,
                        stock = variant.SoLuongTon,
                        imageIds = variant.HinhAnhBienThes.Select(image => image.IdhinhAnh).ToList()
                    })
                    .ToList(),
                images = product.BienTheSanPhams
                    .SelectMany(variant => variant.HinhAnhBienThes)
                    .Select(image => new
                    {
                        id = image.IdhinhAnh,
                        url = image.IdhinhAnhNavigation.DuongDan
                    })
                    .Distinct()
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (product == null)
        {
            return Json(new { success = false, message = "Sản phẩm không tồn tại." });
        }

        return Json(new { success = true, data = product });
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart(int variantId, int quantity)
    {
        if (quantity < 1)
        {
            return Json(new { success = false, message = "Số lượng không hợp lệ." });
        }

        var cart = GetCart();
        var variant = await GetVariantForCartAsync(variantId);

        if (variant == null)
        {
            return Json(new { success = false, message = "Sản phẩm không hợp lệ." });
        }

        var existingQuantity = cart.FirstOrDefault(item => item.IdbienThe == variantId)?.SoLuong ?? 0;
        if (variant.SoLuongTon < existingQuantity + quantity)
        {
            return Json(new { success = false, message = "Số lượng tồn kho không đủ." });
        }

        var finalPrice = GetFinalPrice(
            variant.IdsanPhamNavigation.Gia,
            variant.IdsanPhamNavigation.GiaKhuyenMai,
            variant.IdsanPhamNavigation.NgayBatDauKm,
            variant.IdsanPhamNavigation.NgayKetThucKm);

        var cartItem = cart.FirstOrDefault(item => item.IdbienThe == variantId);
        if (cartItem != null)
        {
            cartItem.SoLuong += quantity;
            cartItem.DonGia = finalPrice;
        }
        else
        {
            cart.Add(new CartItemViewModel
            {
                IdbienThe = variant.IdbienThe,
                TenSanPham = variant.IdsanPhamNavigation.TenSanPham,
                TenMau = variant.IdmauSacNavigation?.TenMau,
                TenKichThuoc = variant.IdkichThuocNavigation?.TenKichThuoc,
                DonGia = finalPrice,
                SoLuong = quantity,
                AnhDaiDien = GetVariantImageUrl(variant)
            });
        }

        SaveCart(CartSessionKey, cart);

        return Json(new
        {
            success = true,
            message = "Thêm vào giỏ hàng thành công!",
            cartCount = cart.Count
        });
    }

    [HttpPost]
    public async Task<IActionResult> BuyNow(int variantId, int quantity)
    {
        if (quantity < 1)
        {
            return Json(new { success = false, message = "Số lượng không hợp lệ." });
        }

        var variant = await GetVariantForCartAsync(variantId);

        if (variant == null)
        {
            return Json(new { success = false, message = "Sản phẩm không hợp lệ." });
        }

        if (variant.SoLuongTon < quantity)
        {
            return Json(new { success = false, message = "Số lượng tồn kho không đủ." });
        }

        var finalPrice = GetFinalPrice(
            variant.IdsanPhamNavigation.Gia,
            variant.IdsanPhamNavigation.GiaKhuyenMai,
            variant.IdsanPhamNavigation.NgayBatDauKm,
            variant.IdsanPhamNavigation.NgayKetThucKm);

        var buyNowCart = new List<CartItemViewModel>
        {
            new()
            {
                IdbienThe = variant.IdbienThe,
                TenSanPham = variant.IdsanPhamNavigation.TenSanPham,
                TenMau = variant.IdmauSacNavigation?.TenMau,
                TenKichThuoc = variant.IdkichThuocNavigation?.TenKichThuoc,
                DonGia = finalPrice,
                SoLuong = quantity,
                AnhDaiDien = GetVariantImageUrl(variant)
            }
        };

        SaveCart(BuyNowCartSessionKey, buyNowCart);

        return Json(new { success = true, redirectUrl = Url.Action("Checkout", "Order") });
    }

    [HttpGet]
    public IActionResult GetCartOffcanvas()
    {
        var cart = GetCart();
        return PartialView("_CartOffcanvasPartial", cart);
    }

    [HttpGet]
    public IActionResult CartIcon()
    {
        var cart = GetCart();
        ViewBag.CartItemCount = cart.Count;

        return PartialView("_CartIconPartial");
    }

    [HttpGet]
    public IActionResult GetCartItemCount()
    {
        var cart = GetCart();
        return Json(new { success = true, count = cart.Count });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateCart(int variantId, int quantity)
    {
        if (quantity < 1)
        {
            return Json(new { success = false, message = "Số lượng không hợp lệ." });
        }

        var cart = GetCart();
        var cartItem = cart.FirstOrDefault(item => item.IdbienThe == variantId);

        if (cartItem == null)
        {
            return Json(new { success = false, message = "Sản phẩm không có trong giỏ hàng." });
        }

        var stock = await dbContext.BienTheSanPhams
            .AsNoTracking()
            .Where(variant => variant.IdbienThe == variantId)
            .Select(variant => variant.SoLuongTon)
            .FirstOrDefaultAsync();

        if (stock < quantity)
        {
            return Json(new { success = false, message = $"Chỉ còn {stock} sản phẩm." });
        }

        cartItem.SoLuong = quantity;
        SaveCart(CartSessionKey, cart);

        return Json(new
        {
            success = true,
            itemTotal = cartItem.ThanhTien.ToString("N0"),
            cartTotal = cart.Sum(item => item.ThanhTien).ToString("N0"),
            cartCount = cart.Count
        });
    }

    [HttpPost]
    public IActionResult RemoveFromCart(int variantId)
    {
        var cart = GetCart();
        var cartItem = cart.FirstOrDefault(item => item.IdbienThe == variantId);

        if (cartItem == null)
        {
            return Json(new { success = false, message = "Sản phẩm không có trong giỏ hàng." });
        }

        cart.Remove(cartItem);
        SaveCart(CartSessionKey, cart);

        return Json(new
        {
            success = true,
            cartTotal = cart.Sum(item => item.ThanhTien).ToString("N0"),
            cartCount = cart.Count
        });
    }

    private async Task<Models.Generated.BienTheSanPham?> GetVariantForCartAsync(int variantId)
    {
        return await dbContext.BienTheSanPhams
            .Include(variant => variant.IdsanPhamNavigation)
            .Include(variant => variant.IdmauSacNavigation)
            .Include(variant => variant.IdkichThuocNavigation)
            .Include(variant => variant.HinhAnhBienThes)
                .ThenInclude(image => image.IdhinhAnhNavigation)
            .FirstOrDefaultAsync(variant => variant.IdbienThe == variantId);
    }

    private List<CartItemViewModel> GetCart()
    {
        var cartJson = HttpContext.Session.GetString(CartSessionKey);

        if (string.IsNullOrWhiteSpace(cartJson))
        {
            return new List<CartItemViewModel>();
        }

        return JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson) ?? new List<CartItemViewModel>();
    }

    private void SaveCart(string sessionKey, List<CartItemViewModel> cart)
    {
        HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(cart));
    }

    private static decimal GetFinalPrice(
        decimal originalPrice,
        decimal? salePrice,
        DateTime? saleStart,
        DateTime? saleEnd)
    {
        var now = DateTime.Now;

        if (salePrice.HasValue
            && saleStart.HasValue
            && saleEnd.HasValue
            && now >= saleStart.Value
            && now <= saleEnd.Value)
        {
            return salePrice.Value;
        }

        return originalPrice;
    }

    private static string GetVariantImageUrl(Models.Generated.BienTheSanPham variant)
    {
        return variant.HinhAnhBienThes
            .Where(image => image.LaAnhChinh == true)
            .Select(image => image.IdhinhAnhNavigation.DuongDan)
            .FirstOrDefault()
            ?? variant.HinhAnhBienThes
                .Select(image => image.IdhinhAnhNavigation.DuongDan)
                .FirstOrDefault()
            ?? "/images/placeholder.png";
    }
}