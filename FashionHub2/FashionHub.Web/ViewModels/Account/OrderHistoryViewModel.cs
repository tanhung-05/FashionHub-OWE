namespace FashionHub.Web.ViewModels.Account
{
    public class OrderHistoryViewModel
    {
        public int IddonHang { get; set; }
        public DateTime? NgayTao { get; set; }
        public decimal TongThanhToan { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public string MauTrangThai { get; set; } = string.Empty;
        public int SoLuongSanPham { get; set; }
    }

    public class OrderDetailViewModel
    {
        public int IddonHang { get; set; }
        public DateTime? NgayTao { get; set; }
        public string TenNguoiNhan { get; set; } = string.Empty;
        public string DiaChiGiao { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public decimal TongTienHang { get; set; }
        public decimal? PhiVanChuyen { get; set; }
        public decimal? TienGiamGia { get; set; }
        public decimal TongThanhToan { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public int IdtrangThai { get; set; }
        public string? PhuongThucThanhToan { get; set; }
        public List<OrderItemViewModel> Items { get; set; } = new();
    }

    public class OrderItemViewModel
    {
        public string TenSanPham { get; set; } = string.Empty;
        public string? HinhAnh { get; set; }
        public string? MauSac { get; set; }
        public string? KichThuoc { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
    }
}