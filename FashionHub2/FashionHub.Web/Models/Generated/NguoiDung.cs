using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class NguoiDung
{
    public int IdnguoiDung { get; set; }

    public string HoTen { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? SoDienThoai { get; set; }

    public string MatKhauHash { get; set; } = null!;

    public Guid SecurityStamp { get; set; }

    public int IdvaiTro { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public bool TrangThai { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<AdminActivityLog> AdminActivityLogs { get; set; } = new List<AdminActivityLog>();

    public virtual ICollection<DanhGia> DanhGia { get; set; } = new List<DanhGia>();

    public virtual ICollection<DatLaiMatKhauToken> DatLaiMatKhauTokens { get; set; } = new List<DatLaiMatKhauToken>();

    public virtual ICollection<DiaChi> DiaChis { get; set; } = new List<DiaChi>();

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();

    public virtual ICollection<GioHang> GioHangs { get; set; } = new List<GioHang>();

    public virtual ICollection<LichSuDonHang> LichSuDonHangs { get; set; } = new List<LichSuDonHang>();

    public virtual ICollection<LichSuTonKho> LichSuTonKhos { get; set; } = new List<LichSuTonKho>();

    public virtual VaiTro IdvaiTroNavigation { get; set; } = null!;

    public virtual ICollection<YeuThich> YeuThiches { get; set; } = new List<YeuThich>();
}
