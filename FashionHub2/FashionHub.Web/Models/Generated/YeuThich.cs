using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class YeuThich
{
    public int IdnguoiDung { get; set; }

    public int IdsanPham { get; set; }

    public DateTime NgayThem { get; set; }

    public virtual NguoiDung IdnguoiDungNavigation { get; set; } = null!;

    public virtual SanPham IdsanPhamNavigation { get; set; } = null!;
}
