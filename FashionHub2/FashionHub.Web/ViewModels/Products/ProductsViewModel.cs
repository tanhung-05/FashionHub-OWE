using FashionHub.Web.Models.Generated;

namespace FashionHub.Web.ViewModels.Products;

public class ProductsViewModel
{
    public List<ProductCardViewModel> Products { get; set; } = new();
    public List<DanhMuc> Categories { get; set; } = new();
    public List<MauSac> Colors { get; set; } = new();
    public List<KichThuoc> Sizes { get; set; } = new();

    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }

    public int? SelectedCategoryId { get; set; }
    public List<int> SelectedColorIds { get; set; } = new();
    public List<int> SelectedSizeIds { get; set; } = new();
    public string? SelectedPriceRange { get; set; }
    public string? SelectedSortBy { get; set; }
    public string? searchString { get; set; }
}