// ViewModels/ProductsViewModel.cs
using FashionHub.Models; 
using System.Collections.Generic;

namespace FashionHub.ViewModels
{
    public class ProductsViewModel
    {
  
        public List<ProductCardViewModel> Products { get; set; }


        public List<DanhMuc> Categories { get; set; }
        public List<MauSac> Colors { get; set; }
        public List<KichThuoc> Sizes { get; set; }


        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public int? SelectedCategoryId { get; set; }
        public List<int> SelectedColorIds { get; set; }
        public List<int> SelectedSizeIds { get; set; }
        public string SelectedPriceRange { get; set; }
        public string SelectedSortBy { get; set; }

        public string searchString { get; set; }
        public ProductsViewModel()
        {
            SelectedColorIds = new List<int>();
            SelectedSizeIds = new List<int>();
        }
    }
}