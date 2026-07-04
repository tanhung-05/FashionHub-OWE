// Trong thư mục: ViewModels/HomeViewModel.cs
using System.Collections.Generic;

namespace FashionHub.ViewModels
{
    public class HomeViewModel
    {
        public List<ProductCardViewModel> SanPhamMoi { get; set; }
        public List<ProductCardViewModel> SanPhamNoiBat { get; set; }
    }
}