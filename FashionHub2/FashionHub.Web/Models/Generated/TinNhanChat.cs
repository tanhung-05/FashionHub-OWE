namespace FashionHub.Web.Models.Generated;

public partial class TinNhanChat
{
    public long IdtinNhan { get; set; }

    public Guid IdcuocTroChuyen { get; set; }

    public string VaiTro { get; set; } = null!;

    public string NoiDung { get; set; } = null!;

    public string? DuLieuJson { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual CuocTroChuyen IdcuocTroChuyenNavigation { get; set; } = null!;
}
