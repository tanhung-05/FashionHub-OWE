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

    public int IdvaiTro { get; set; }

    public DateTime? NgayTao { get; set; }

    public bool? TrangThai { get; set; }

    public virtual ICollection<DiaChi> DiaChis { get; set; } = new List<DiaChi>();

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();

    public virtual ICollection<GioHang> GioHangs { get; set; } = new List<GioHang>();

    public virtual VaiTro IdvaiTroNavigation { get; set; } = null!;
}
