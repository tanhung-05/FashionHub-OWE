using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class KichThuoc
{
    public int IdkichThuoc { get; set; }

    public string TenKichThuoc { get; set; } = null!;

    public virtual ICollection<BienTheSanPham> BienTheSanPhams { get; set; } = new List<BienTheSanPham>();
}
