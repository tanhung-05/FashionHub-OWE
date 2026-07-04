// ViewModels/ProductVariantViewModel.cs
using System.Collections.Generic;

namespace FashionHub.ViewModels
{
    public class ProductVariantViewModel
    {
        public int IDBienThe { get; set; }
        public int? IDMauSac { get; set; }
        public int? IDKichThuoc { get; set; }
        public decimal Gia { get; set; }
        public decimal? GiaKhuyenMai { get; set; }
        public int SoLuongTon { get; set; }
        public string Sku { get; set; }
        public List<int> HinhAnhIDs { get; set; }
        public string SearchString { get; set; }
    }
}