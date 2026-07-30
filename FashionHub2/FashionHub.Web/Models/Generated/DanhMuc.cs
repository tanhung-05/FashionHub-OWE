using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class DanhMuc
{
    public int IddanhMuc { get; set; }

    public string TenDanhMuc { get; set; } = null!;

    public string? Slug { get; set; }

    public int? IddanhMucCha { get; set; }

    public int ThuTuHienThi { get; set; }

    public bool TrangThai { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual DanhMuc? IddanhMucChaNavigation { get; set; }

    public virtual ICollection<DanhMuc> InverseIddanhMucChaNavigation { get; set; } = new List<DanhMuc>();

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}
