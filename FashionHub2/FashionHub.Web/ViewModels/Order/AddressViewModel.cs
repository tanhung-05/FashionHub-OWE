namespace FashionHub.Web.ViewModels.Order;

public class AddressViewModel
{
    public int IddiaChi { get; set; }
    
    public string TenNguoiNhan { get; set; } = string.Empty;
    
    public string SoDienThoai { get; set; } = string.Empty;
    
    public bool LaMacDinh { get; set; }
    
    public string FullAddress { get; set; } = string.Empty;
}