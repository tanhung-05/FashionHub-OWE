namespace FashionHub.Web.ViewModels.Cart;

public class CartItemViewModel
{
    public int IdbienThe { get; set; }

    public string TenSanPham { get; set; } = string.Empty;

    public string? TenMau { get; set; }

    public string? TenKichThuoc { get; set; }

    public decimal DonGia { get; set; }

    public int SoLuong { get; set; }

    public string AnhDaiDien { get; set; } = "/images/placeholder.png";

    public decimal ThanhTien => SoLuong * DonGia;
}