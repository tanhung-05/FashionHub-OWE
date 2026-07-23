using System.ComponentModel.DataAnnotations;

namespace FashionHub.Web.Areas.Admin.ViewModels
{
    public class CategoryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự")]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Danh mục cha")]
        public int? ParentCategoryId { get; set; }

        [Display(Name = "Danh mục cha")]
        public string? ParentCategoryName { get; set; }

        public int ProductCount { get; set; }
        public List<CategoryViewModel> SubCategories { get; set; } = new();
    }

    public class CategoryListViewModel
    {
        public List<CategoryViewModel> Categories { get; set; } = new();
        public string? SearchTerm { get; set; }
    }
}