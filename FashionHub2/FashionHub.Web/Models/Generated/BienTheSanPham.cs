using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class BienTheSanPham
{
    public int IdbienThe { get; set; }

    public int IdsanPham { get; set; }

    public int? IdmauSac { get; set; }

    public int? IdkichThuoc { get; set; }

    public string Sku { get; set; } = null!;

    public decimal Gia { get; set; }

    public int SoLuongTon { get; set; }

    public int SoLuongCanhBao { get; set; }

    public int TongDaBan { get; set; }

    public bool TrangThai { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public DateTime? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    public virtual ICollection<GioHang> GioHangs { get; set; } = new List<GioHang>();

    public virtual ICollection<HinhAnhBienThe> HinhAnhBienThes { get; set; } = new List<HinhAnhBienThe>();

    public virtual ICollection<LichSuTonKho> LichSuTonKhos { get; set; } = new List<LichSuTonKho>();

    public virtual KichThuoc? IdkichThuocNavigation { get; set; }

    public virtual MauSac? IdmauSacNavigation { get; set; }

    public virtual SanPham IdsanPhamNavigation { get; set; } = null!;
}
