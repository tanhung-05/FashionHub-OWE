using FashionHub.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace FashionHub.Areas.Admin.ViewModels
{
    // Dùng cho trang Thêm mới/Sửa thông tin chung
    public class ProductViewModel
    {
        public int IDSanPham { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        public string TenSanPham { get; set; }

        [AllowHtml] 
        public string MoTa { get; set; }

        [Required]
        public int IDDanhMuc { get; set; }

        [Required]
        public int IDThuongHieu { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá bán")]
        [Range(1000, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn 1.000đ")]
        public decimal Gia { get; set; }
        public bool TrangThai { get; set; }
    }

    // Dùng cho Modal thêm biến thể
    public class ProductVariantViewModel
    {
        public int IDSanPham { get; set; }

        [Required]
        public int IDMauSac { get; set; }

        [Required]
        public int IDKichThuoc { get; set; }

        [Required]

        public int SoLuongTon { get; set; }

        // File ảnh upload lên
        public HttpPostedFileBase UploadImage { get; set; }
    }
}