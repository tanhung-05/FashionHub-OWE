namespace FashionHub.Web.Models.Generated;

public partial class CuocTroChuyen
{
    public Guid IdcuocTroChuyen { get; set; }

    public int IdnguoiDung { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public DateTime? NgayKetThuc { get; set; }

    public virtual NguoiDung IdnguoiDungNavigation { get; set; } = null!;

    public virtual ICollection<TinNhanChat> TinNhanChats { get; set; } =
        new List<TinNhanChat>();
}
