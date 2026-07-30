using FashionHub.Web.Application.Cart;

namespace FashionHub.Web.ViewModels.Cart;

public static class CartMvcMapper
{
    public static List<CartItemViewModel> ToViewModels(this CartDto cart)
    {
        return cart.Items.Select(item => new CartItemViewModel
        {
            IdsanPham = item.ProductId,
            IdbienThe = item.VariantId,
            TenSanPham = item.ProductName,
            TenMau = item.ColorName,
            TenKichThuoc = item.SizeName,
            DonGia = item.UnitPrice,
            SoLuong = item.Quantity,
            AnhDaiDien = item.ImageUrl,
            SoLuongTon = item.AvailableStock
        }).ToList();
    }
}
