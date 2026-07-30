using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class LichSuDonHang
{
    public int IdlichSu { get; set; }

    public int IddonHang { get; set; }

    public int? IdtrangThaiCu { get; set; }

    public int IdtrangThaiMoi { get; set; }

    public int? IdnguoiThucHien { get; set; }

    public string? GhiChu { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual DonHang IddonHangNavigation { get; set; } = null!;

    public virtual NguoiDung? IdnguoiThucHienNavigation { get; set; }

    public virtual TrangThaiDonHang? IdtrangThaiCuNavigation { get; set; }

    public virtual TrangThaiDonHang IdtrangThaiMoiNavigation { get; set; } = null!;
}
