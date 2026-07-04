using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class MaGiamGium
{
    public int IdmaGiamGia { get; set; }

    public string MaCode { get; set; } = null!;

    public string? TenChuongTrinh { get; set; }

    public int LoaiGiamGia { get; set; }

    public decimal GiaTri { get; set; }

    public decimal? DonHangToiThieu { get; set; }

    public decimal? GiamToiDa { get; set; }

    public int SoLuong { get; set; }

    public int DaSuDung { get; set; }

    public DateTime? NgayBatDau { get; set; }

    public DateTime? NgayKetThuc { get; set; }

    public bool TrangThai { get; set; }

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
}
