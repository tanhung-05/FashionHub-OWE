using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FashionHub.Web.Areas.Admin.ViewModels
{
    /// <summary>
    /// ViewModel cho tạo mới/chỉnh sửa sản phẩm
    /// </summary>
    public class ProductAdminViewModel
    {
        public int IDSanPham { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [Display(Name = "Tên sản phẩm")]
        public string TenSanPham { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        public string? MoTa { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int IDDanhMuc { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thương hiệu")]
        [Display(Name = "Thương hiệu")]
        public int IDThuongHieu { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá bán")]
        [Range(1000, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn 1.000đ")]
        [Display(Name = "Giá bán")]
        public decimal Gia { get; set; }

        [Display(Name = "Trạng thái")]
        public bool TrangThai { get; set; } = true;

        // For display only
        public string? TenDanhMuc { get; set; }
        public string? TenThuongHieu { get; set; }
        
        // Variants for Edit view
        public List<VariantDetailViewModel> Variants { get; set; } = new();
    }

    /// <summary>
    /// ViewModel cho thêm biến thể sản phẩm
    /// </summary>
    public class ProductVariantAdminViewModel
    {
        [Required]
        public int IDSanPham { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn màu sắc")]
        [Display(Name = "Màu sắc")]
        public int IDMauSac { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn kích thước")]
        [Display(Name = "Kích thước")]
        public int IDKichThuoc { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng tồn")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn không được âm")]
        [Display(Name = "Số lượng tồn")]
        public int SoLuongTon { get; set; }

        [Display(Name = "Ảnh sản phẩm")]
        public IFormFile? UploadImage { get; set; }
    }

    /// <summary>
    /// ViewModel cho danh sách sản phẩm với filter
    /// </summary>
    public class ProductListAdminViewModel
    {
        public List<ProductItemAdminViewModel> Products { get; set; } = new();
        
        // Filter parameters
        public string? SearchString { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public int? Status { get; set; }
    }

    /// <summary>
    /// ViewModel cho từng sản phẩm trong danh sách
    /// </summary>
    public class ProductItemAdminViewModel
    {
        public int IDSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string? TenDanhMuc { get; set; }
        public string? TenThuongHieu { get; set; }
        public decimal Gia { get; set; }
        public decimal? GiaKhuyenMai { get; set; }
        public bool TrangThai { get; set; }
        public int VariantCount { get; set; }
        public int TotalStock { get; set; }
        public string? MainImageUrl { get; set; }
    }

    /// <summary>
    /// ViewModel cho quản lý biến thể trong trang Edit
    /// </summary>
    public class VariantDetailViewModel
    {
        public int IDBienThe { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string TenMau { get; set; } = string.Empty;
        public string TenKichThuoc { get; set; } = string.Empty;
        public int SoLuongTon { get; set; }
        public List<VariantImageViewModel> Images { get; set; } = new();
    }

    /// <summary>
    /// ViewModel cho ảnh biến thể
    /// </summary>
    public class VariantImageViewModel
    {
        public int IDHinhAnh { get; set; }
        public string DuongDan { get; set; } = string.Empty;
        public bool LaAnhChinh { get; set; }
    }
}