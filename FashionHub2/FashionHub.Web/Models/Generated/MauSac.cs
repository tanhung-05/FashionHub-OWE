using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class MauSac
{
    public int IdmauSac { get; set; }

    public string TenMau { get; set; } = null!;

    public string? MaMauHex { get; set; }

    public virtual ICollection<BienTheSanPham> BienTheSanPhams { get; set; } = new List<BienTheSanPham>();
}
