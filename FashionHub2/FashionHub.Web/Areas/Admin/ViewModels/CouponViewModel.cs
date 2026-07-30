using System.ComponentModel.DataAnnotations;
using FashionHub.Web.Domain;

namespace FashionHub.Web.Areas.Admin.ViewModels
{
    public class CouponViewModel
    {
        public int IdmaGiamGia { get; set; }

        [Required(ErrorMessage = "Mã code là bắt buộc")]
        [Display(Name = "Mã giảm giá")]
        [StringLength(50)]
        public string MaCode { get; set; } = null!;

        [Display(Name = "Tên chương trình")]
        [StringLength(200)]
        public string? TenChuongTrinh { get; set; }

        [Required(ErrorMessage = "Loại giảm giá là bắt buộc")]
        [Display(Name = "Loại giảm giá")]
        public int LoaiGiamGia { get; set; }

        [Required(ErrorMessage = "Giá trị là bắt buộc")]
        [Display(Name = "Giá trị")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Giá trị phải lớn hơn 0")]
        public decimal GiaTri { get; set; }

        [Display(Name = "Đơn hàng tối thiểu")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá trị phải lớn hơn hoặc bằng 0")]
        public decimal? DonHangToiThieu { get; set; }

        [Display(Name = "Giảm tối đa")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá trị phải lớn hơn hoặc bằng 0")]
        public decimal? GiamToiDa { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Display(Name = "Số lượng")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int SoLuong { get; set; }

        [Display(Name = "Đã sử dụng")]
        public int DaSuDung { get; set; }

        [Display(Name = "Ngày bắt đầu")]
        public DateTime? NgayBatDau { get; set; }

        [Display(Name = "Ngày kết thúc")]
        public DateTime? NgayKetThuc { get; set; }

        [Display(Name = "Trạng thái")]
        public bool TrangThai { get; set; }

        // Helper properties
        public string LoaiGiamGiaText => LoaiGiamGia == CouponTypes.FixedAmount ? "Số tiền" : "Phần trăm";
        public string TrangThaiText => TrangThai ? "Hoạt động" : "Tắt";
        public bool IsExpired => NgayKetThuc.HasValue && NgayKetThuc.Value < DateTime.Now;
        public bool IsOutOfStock => DaSuDung >= SoLuong;
    }
}
