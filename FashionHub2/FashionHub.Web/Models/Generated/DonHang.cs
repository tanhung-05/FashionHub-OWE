using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class DonHang
{
    public int IddonHang { get; set; }

    public int? IdnguoiDung { get; set; }

    public int? IdmaGiamGia { get; set; }

    public string TenNguoiNhan { get; set; } = null!;

    public string DiaChiGiao { get; set; } = null!;

    public string SoDienThoai { get; set; } = null!;

    public decimal TongTienHang { get; set; }

    public decimal PhiVanChuyen { get; set; }

    public decimal TienGiamGia { get; set; }

    public decimal TongThanhToan { get; set; }

    public int? IdphuongThucThanhToan { get; set; }

    public int IdtrangThai { get; set; }

    public string? GhiChu { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    public virtual ICollection<LichSuDonHang> LichSuDonHangs { get; set; } = new List<LichSuDonHang>();

    public virtual ICollection<LichSuTonKho> LichSuTonKhos { get; set; } = new List<LichSuTonKho>();

    public virtual MaGiamGium? IdmaGiamGiaNavigation { get; set; }

    public virtual NguoiDung? IdnguoiDungNavigation { get; set; }

    public virtual PhuongThucThanhToan? IdphuongThucThanhToanNavigation { get; set; }

    public virtual TrangThaiDonHang IdtrangThaiNavigation { get; set; } = null!;
}
