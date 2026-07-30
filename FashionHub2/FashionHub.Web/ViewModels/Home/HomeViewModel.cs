using FashionHub.Web.ViewModels.Products;
using FashionHub.Web.Models.Generated;

namespace FashionHub.Web.ViewModels.Home;

public class HomeViewModel
{
    public List<ProductCardViewModel> SanPhamMoi { get; set; } = new();
    public List<ProductCardViewModel> SanPhamKhuyenMai { get; set; } = new();
    public List<DanhMuc> DanhMuc { get; set; } = new();
    public List<ThuongHieu> ThuongHieu { get; set; } = new();
}
