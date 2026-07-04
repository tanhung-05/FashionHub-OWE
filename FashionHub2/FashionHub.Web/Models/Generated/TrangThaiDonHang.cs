using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class TrangThaiDonHang
{
    public int IdtrangThai { get; set; }

    public string TenTrangThai { get; set; } = null!;

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
}
