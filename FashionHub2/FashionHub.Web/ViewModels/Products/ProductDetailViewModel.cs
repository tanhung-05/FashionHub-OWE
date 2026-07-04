using FashionHub.Web.Models.Generated;

namespace FashionHub.Web.ViewModels.Products;

public class ProductDetailViewModel
{
    public int IDSanPham { get; set; }
    public string TenSanPham { get; set; } = string.Empty;
    public decimal Gia { get; set; }
    public string? MoTa { get; set; }
    public string TenDanhMuc { get; set; } = string.Empty;
    public int IDDanhMuc { get; set; }
    public string? TenThuongHieu { get; set; }
    public decimal? GiaKhuyenMai { get; set; }
    public DateTime? NgayBatDauKM { get; set; }
    public DateTime? NgayKetThucKM { get; set; }

    // Dữ liệu cho việc chọn biến thể
    public List<MauSac> AvailableColors { get; set; } = new();
    public List<KichThuoc> AvailableSizes { get; set; } = new();
    public List<HinhAnh> AllImages { get; set; } = new();

    // Dữ liệu JSON cho JavaScript
    public string VariantsJson { get; set; } = string.Empty;

    // Sản phẩm liên quan
    public List<ProductCardViewModel> RelatedProducts { get; set; } = new();
    public bool IsOutStock { get; set; }

    public bool IsSaleActive
    {
        get
        {
            return GiaKhuyenMai.HasValue
                   && GiaKhuyenMai < Gia
                   && NgayBatDauKM.HasValue
                   && NgayKetThucKM.HasValue
                   && DateTime.Now >= NgayBatDauKM.Value
                   && DateTime.Now <= NgayKetThucKM.Value;
        }
    }
}