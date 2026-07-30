using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class TrangThaiDonHang
{
    public int IdtrangThai { get; set; }

    public string TenTrangThai { get; set; } = null!;

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();

    public virtual ICollection<LichSuDonHang> LichSuDonHangTrangThaiCus { get; set; } = new List<LichSuDonHang>();

    public virtual ICollection<LichSuDonHang> LichSuDonHangTrangThaiMois { get; set; } = new List<LichSuDonHang>();
}
