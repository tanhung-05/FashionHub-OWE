using System.Text.Json;
using FashionHub.Web.Data;
using FashionHub.Web.Services;
using FashionHub.Web.ViewModels.Cart;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Controllers;

public class CartController : Controller
{
    private const string BuyNowCartSessionKey = "BuyNowCart";
    private readonly ApplicationDbContext dbContext;
    private readonly ICartService cartService;

    public CartController(
        ApplicationDbContext dbContext,
        ICartService cartService)
    {
        this.dbContext = dbContext;
        this.cartService = cartService;
    }

    public async Task<IActionResult> Index()
    {
        var result = await cartService.GetCartAsync();
        return View(result.Value?.ToViewModels() ?? new List<CartItemViewModel>());
    }

    [HttpGet]
    public async Task<IActionResult> GetProductDetails(int productId)
    {
        var product = await dbContext.SanPhams
            .AsNoTracking()
            .Where(product => product.IdsanPham == productId
                && product.TrangThai == true
                && product.DeletedAt == null)
            .Select(product => new
            {
                id = product.IdsanPham,
                name = product.TenSanPham,
                variants = product.BienTheSanPhams
                    .Where(variant => variant.TrangThai
                        && variant.DeletedAt == null
                        && variant.SoLuongTon > 0)
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
                    .Where(variant => variant.TrangThai && variant.DeletedAt == null)
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

        var result = await cartService.AddAsync(variantId, quantity);
        if (!result.IsSuccess)
        {
            if (result.Error?.Code == "cart_variant_not_found")
            {
                return BadRequest(new { success = false, message = result.Error.Message });
            }

            return Json(new { success = false, message = result.Error?.Message });
        }

        return Json(new
        {
            success = true,
            message = "Thêm vào giỏ hàng thành công!",
            cartCount = result.Value!.TotalQuantity
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
    public async Task<IActionResult> GetCartOffcanvas()
    {
        var result = await cartService.GetCartAsync();
        return PartialView(
            "_CartOffcanvasPartial",
            result.Value?.ToViewModels() ?? new List<CartItemViewModel>());
    }

    [HttpGet]
    public async Task<IActionResult> CartIcon()
    {
        var result = await cartService.GetCartAsync();
        ViewBag.CartItemCount = result.Value?.TotalQuantity ?? 0;

        return PartialView("_CartIconPartial");
    }

    [HttpGet]
    public async Task<IActionResult> GetCartItemCount()
    {
        var result = await cartService.GetCartAsync();
        return Json(new
        {
            success = result.IsSuccess,
            count = result.Value?.TotalQuantity ?? 0
        });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateCart(int variantId, int quantity)
    {
        if (quantity < 1)
        {
            return Json(new { success = false, message = "Số lượng không hợp lệ." });
        }

        var result = await cartService.UpdateAsync(variantId, quantity);
        if (!result.IsSuccess)
        {
            return Json(new { success = false, message = result.Error?.Message });
        }

        var cartItem = result.Value!.Items.First(item => item.VariantId == variantId);

        return Json(new
        {
            success = true,
            itemTotal = cartItem.LineTotal.ToString("N0"),
            cartTotal = result.Value.Subtotal.ToString("N0"),
            cartCount = result.Value.TotalQuantity
        });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveFromCart(int variantId)
    {
        var result = await cartService.RemoveAsync(variantId);
        if (!result.IsSuccess)
        {
            return Json(new { success = false, message = result.Error?.Message });
        }

        return Json(new
        {
            success = true,
            cartTotal = result.Value!.Subtotal.ToString("N0"),
            cartCount = result.Value.TotalQuantity
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
            .FirstOrDefaultAsync(variant =>
                variant.IdbienThe == variantId
                && variant.TrangThai
                && variant.DeletedAt == null
                && variant.IdsanPhamNavigation.TrangThai
                && variant.IdsanPhamNavigation.DeletedAt == null);
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
