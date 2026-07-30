using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class ChiTietDonHang
{
    public int IdchiTietDonHang { get; set; }

    public int IddonHang { get; set; }

    public int? IdbienThe { get; set; }

    public int SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public string TenSanPham { get; set; } = null!;

    public string? TenMau { get; set; }

    public string? TenKichThuoc { get; set; }

    public virtual BienTheSanPham? IdbienTheNavigation { get; set; }

    public virtual ICollection<DanhGia> DanhGia { get; set; } = new List<DanhGia>();

    public virtual DonHang IddonHangNavigation { get; set; } = null!;
}
