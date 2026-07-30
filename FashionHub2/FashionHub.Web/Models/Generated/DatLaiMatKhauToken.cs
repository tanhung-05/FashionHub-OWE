namespace FashionHub.Web.Models.Generated;

public partial class DatLaiMatKhauToken
{
    public long Idtoken { get; set; }

    public int IdnguoiDung { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime NgayHetHanUtc { get; set; }

    public DateTime NgayTaoUtc { get; set; }

    public DateTime? NgaySuDungUtc { get; set; }

    public string? DiaChiIp { get; set; }

    public virtual NguoiDung IdnguoiDungNavigation { get; set; } = null!;
}
