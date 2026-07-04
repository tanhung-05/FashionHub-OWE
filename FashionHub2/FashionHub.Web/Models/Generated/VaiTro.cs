using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class VaiTro
{
    public int IdvaiTro { get; set; }

    public string TenVaiTro { get; set; } = null!;

    public virtual ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();
}
