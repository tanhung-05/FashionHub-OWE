using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class ThuongHieu
{
    public int IdthuongHieu { get; set; }

    public string TenThuongHieu { get; set; } = null!;

    public bool TrangThai { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}
