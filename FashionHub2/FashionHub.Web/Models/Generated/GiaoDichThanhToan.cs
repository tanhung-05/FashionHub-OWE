using System;

namespace FashionHub.Web.Models.Generated;

public partial class GiaoDichThanhToan
{
    public long IdgiaoDich { get; set; }

    public int IddonHang { get; set; }

    public string MaThamChieu { get; set; } = null!;

    public string CongThanhToan { get; set; } = null!;

    public decimal SoTien { get; set; }

    public byte TrangThai { get; set; }

    public string? MaGiaoDichCong { get; set; }

    public string? MaPhanHoi { get; set; }

    public string? MaNganHang { get; set; }

    public string? NoiDung { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public DateTime? NgayThanhToan { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual DonHang IddonHangNavigation { get; set; } = null!;
}
