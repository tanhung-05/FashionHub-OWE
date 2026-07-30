using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class DanhGia
{
    public int IddanhGia { get; set; }

    public int IdnguoiDung { get; set; }

    public int IdsanPham { get; set; }

    public int? IdchiTietDonHang { get; set; }

    public byte DiemSo { get; set; }

    public string? NoiDung { get; set; }

    public bool TrangThai { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ChiTietDonHang? IdchiTietDonHangNavigation { get; set; }

    public virtual NguoiDung IdnguoiDungNavigation { get; set; } = null!;

    public virtual SanPham IdsanPhamNavigation { get; set; } = null!;
}
