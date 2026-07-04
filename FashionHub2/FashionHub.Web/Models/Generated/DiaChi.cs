using System;
using System.Collections.Generic;

namespace FashionHub.Web.Models.Generated;

public partial class DiaChi
{
    public int IddiaChi { get; set; }

    public int IdnguoiDung { get; set; }

    public string TenNguoiNhan { get; set; } = null!;

    public string SoDienThoai { get; set; } = null!;

    public string ChiTiet { get; set; } = null!;

    public string PhuongXa { get; set; } = null!;

    public string QuanHuyen { get; set; } = null!;

    public string TinhThanh { get; set; } = null!;

    public bool? LaMacDinh { get; set; }

    public virtual NguoiDung IdnguoiDungNavigation { get; set; } = null!;
}
