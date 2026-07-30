using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class SanPham
{
    public int IdsanPham { get; set; }

    public string TenSanPham { get; set; } = null!;

    public string? Slug { get; set; }

    public string? MoTa { get; set; }

    public decimal Gia { get; set; }

    public decimal? GiaKhuyenMai { get; set; }

    public DateTime? NgayBatDauKm { get; set; }

    public DateTime? NgayKetThucKm { get; set; }

    public int? IddanhMuc { get; set; }

    public int? IdthuongHieu { get; set; }

    public bool TrangThai { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<BienTheSanPham> BienTheSanPhams { get; set; } = new List<BienTheSanPham>();

    public virtual ICollection<DanhGia> DanhGia { get; set; } = new List<DanhGia>();

    public virtual DanhMuc? IddanhMucNavigation { get; set; }

    public virtual ThuongHieu? IdthuongHieuNavigation { get; set; }

    public virtual ICollection<YeuThich> YeuThiches { get; set; } = new List<YeuThich>();
}
