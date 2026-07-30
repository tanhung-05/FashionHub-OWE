using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class AdminActivityLog
{
    public long Idlog { get; set; }

    public int? Idadmin { get; set; }

    public string HanhDong { get; set; } = null!;

    public string? TenBang { get; set; }

    public string? IdbanGhi { get; set; }

    public string? DuLieuCu { get; set; }

    public string? DuLieuMoi { get; set; }

    public string? DiaChiIp { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual NguoiDung? IdadminNavigation { get; set; }
}
