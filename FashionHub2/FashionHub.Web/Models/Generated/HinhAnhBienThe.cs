using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class HinhAnhBienThe
{
    public int IdhinhAnh { get; set; }

    public int IdbienThe { get; set; }

    public bool LaAnhChinh { get; set; }

    public int ThuTuHienThi { get; set; }

    public virtual BienTheSanPham IdbienTheNavigation { get; set; } = null!;

    public virtual HinhAnh IdhinhAnhNavigation { get; set; } = null!;
}
