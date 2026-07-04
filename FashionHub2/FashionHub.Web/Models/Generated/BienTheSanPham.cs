using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class BienTheSanPham
{
    public int IdbienThe { get; set; }

    public int IdsanPham { get; set; }

    public int? IdmauSac { get; set; }

    public int? IdkichThuoc { get; set; }

    public string? Sku { get; set; }

    public decimal Gia { get; set; }

    public int SoLuongTon { get; set; }

    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    public virtual ICollection<GioHang> GioHangs { get; set; } = new List<GioHang>();

    public virtual ICollection<HinhAnhBienThe> HinhAnhBienThes { get; set; } = new List<HinhAnhBienThe>();

    public virtual KichThuoc? IdkichThuocNavigation { get; set; }

    public virtual MauSac? IdmauSacNavigation { get; set; }

    public virtual SanPham IdsanPhamNavigation { get; set; } = null!;
}
