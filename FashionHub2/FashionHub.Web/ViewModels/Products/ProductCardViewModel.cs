namespace FashionHub.Web.ViewModels.Products;

public class ProductCardViewModel
{
    public int IDSanPham { get; set; }
    public string TenSanPham { get; set; } = string.Empty;
    public decimal? Gia { get; set; }
    public decimal? GiaKhuyenMai { get; set; }
    public DateTime? NgayBatDauKM { get; set; }
    public DateTime? NgayKetThucKM { get; set; }
    public string AnhChinhURL { get; set; } = string.Empty;
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