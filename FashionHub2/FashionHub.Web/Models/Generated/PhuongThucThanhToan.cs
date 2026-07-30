using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class PhuongThucThanhToan
{
    public int IdphuongThucThanhToan { get; set; }

    public string TenPhuongThuc { get; set; } = null!;

    public bool TrangThai { get; set; }

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
}
