using FashionHub.Web.ViewModels.Products;

namespace FashionHub.Web.ViewModels.Home;

public class HomeViewModel
{
    public List<ProductCardViewModel> SanPhamMoi { get; set; } = new();
    public List<ProductCardViewModel> SanPhamKhuyenMai { get; set; } = new();
}