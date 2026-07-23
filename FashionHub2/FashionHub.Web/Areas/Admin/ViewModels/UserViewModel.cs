using System.ComponentModel.DataAnnotations;

namespace FashionHub.Web.Areas.Admin.ViewModels
{
    public class UserViewModel
    {
        public int IdnguoiDung { get; set; }

        [Display(Name = "Họ tên")]
        public string HoTen { get; set; } = null!;

        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Display(Name = "Số điện thoại")]
        public string? SoDienThoai { get; set; }

        [Display(Name = "Vai trò")]
        public string VaiTro { get; set; } = null!;

        [Display(Name = "Ngày tạo")]
        public DateTime? NgayTao { get; set; }

        [Display(Name = "Trạng thái")]
        public bool? TrangThai { get; set; }

        [Display(Name = "Tổng đơn hàng")]
        public int TotalOrders { get; set; }

        [Display(Name = "Tổng chi tiêu")]
        public decimal TotalSpent { get; set; }
    }
}