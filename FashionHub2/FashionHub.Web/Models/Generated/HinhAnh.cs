using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class HinhAnh
{
    public int IdhinhAnh { get; set; }

    public string DuongDan { get; set; } = null!;

    public string? MoTa { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual ICollection<HinhAnhBienThe> HinhAnhBienThes { get; set; } = new List<HinhAnhBienThe>();
}
