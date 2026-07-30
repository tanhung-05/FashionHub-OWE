using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class GioHang
{
    public int IdnguoiDung { get; set; }

    public int IdbienThe { get; set; }

    public int SoLuong { get; set; }

    public DateTime NgayThem { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual BienTheSanPham IdbienTheNavigation { get; set; } = null!;

    public virtual NguoiDung IdnguoiDungNavigation { get; set; } = null!;
}
