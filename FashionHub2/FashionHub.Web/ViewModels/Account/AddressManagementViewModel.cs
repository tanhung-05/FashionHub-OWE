using System.ComponentModel.DataAnnotations;

namespace FashionHub.Web.ViewModels.Account
{
    public class AddressManagementViewModel
    {
        public int IdDiaChi { get; set; }

        [Required(ErrorMessage = "Tên người nhận là bắt buộc")]
        [Display(Name = "Tên người nhận")]
        [StringLength(100)]
        public string TenNguoiNhan { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        [Display(Name = "Địa chỉ")]
        [StringLength(200)]
        public string ChiTiet { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phường/Xã là bắt buộc")]
        [Display(Name = "Phường/Xã")]
        [StringLength(100)]
        public string PhuongXa { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quận/Huyện là bắt buộc")]
        [Display(Name = "Quận/Huyện")]
        [StringLength(100)]
        public string QuanHuyen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tỉnh/Thành phố là bắt buộc")]
        [Display(Name = "Tỉnh/Thành phố")]
        [StringLength(100)]
        public string TinhThanh { get; set; } = string.Empty;

        [Display(Name = "Đặt làm địa chỉ mặc định")]
        public bool LaMacDinh { get; set; }
    }
}