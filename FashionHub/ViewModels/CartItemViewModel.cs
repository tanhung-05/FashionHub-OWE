
namespace FashionHub.ViewModels
{
    public class CartItemViewModel
    {
        public int IDBienThe { get; set; }
        public string TenSanPham { get; set; }
        public string TenMau { get; set; }
        public string TenKichThuoc { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public string AnhDaiDien { get; set; }
        public decimal ThanhTien => SoLuong * DonGia;
    }
}