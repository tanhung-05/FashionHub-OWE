using System.ComponentModel.DataAnnotations;

namespace FashionHub.Web.ViewModels.Account
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [Display(Name = "Họ tên")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string? SoDienThoai { get; set; }

        public DateTime NgayThamGia { get; set; }

        public string TenVaiTro { get; set; } = "Khách hàng";

        public int SoDonHang { get; set; }

        public int SoDonDangXuLy { get; set; }

        public int SoDiaChi { get; set; }

        public ProfileAddressSummaryViewModel? DiaChiMacDinh { get; set; }
    }

    public class ProfileAddressSummaryViewModel
    {
        public string TenNguoiNhan { get; set; } = string.Empty;

        public string SoDienThoai { get; set; } = string.Empty;

        public string DiaChiDayDu { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
