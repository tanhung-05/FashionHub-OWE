using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class LichSuTonKho
{
    public int IdlichSu { get; set; }

    public int IdbienThe { get; set; }

    public int? IdnguoiThucHien { get; set; }

    public int? IddonHang { get; set; }

    public string LoaiThayDoi { get; set; } = null!;

    public int SoLuongThayDoi { get; set; }

    public int TonTruoc { get; set; }

    public int TonSau { get; set; }

    public string? GhiChu { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual BienTheSanPham IdbienTheNavigation { get; set; } = null!;

    public virtual DonHang? IddonHangNavigation { get; set; }

    public virtual NguoiDung? IdnguoiThucHienNavigation { get; set; }
}
