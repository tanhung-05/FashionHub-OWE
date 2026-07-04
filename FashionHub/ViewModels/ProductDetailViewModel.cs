
using FashionHub.Models;
using System;
using System.Collections.Generic;

namespace FashionHub.ViewModels
{
    public class ProductDetailViewModel
    {
        public int IDSanPham { get; set; }
        public string TenSanPham { get; set; }
        public decimal Gia { get; set; }
        public string MoTa { get; set; }
        public string TenDanhMuc { get; set; }
        public int IDDanhMuc { get; set; }
        public string TenThuongHieu { get; set; }
        public decimal? GiaKhuyenMai { get; set; }
        public DateTime? NgayBatDauKM { get; set; }
        public DateTime? NgayKetThucKM { get; set; }

        // Dữ liệu cho việc chọn biến thể
        public List<MauSac> AvailableColors { get; set; }
        public List<KichThuoc> AvailableSizes { get; set; }
        public List<HinhAnh> AllImages { get; set; }

        // Dữ liệu JSON cho JavaScript
        public string VariantsJson { get; set; }

        // Sản phẩm liên quan
        public List<ProductCardViewModel> RelatedProducts { get; set; }
        public bool IsOutStock { get; set; }

        public bool IsSaleActive
        {
            get
            {
                return GiaKhuyenMai.HasValue
                       && GiaKhuyenMai < Gia
                       && NgayBatDauKM.HasValue
                       && NgayKetThucKM.HasValue
                       && DateTime.Now >= NgayBatDauKM.Value
                       && DateTime.Now <= NgayKetThucKM.Value;
            }
        }
    }
}