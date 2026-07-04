using FashionHub.Models;
using FashionHub.Services;
using FashionHub.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Drawing; // <-- CẦN CÁI NÀY
using System.Drawing.Imaging; // <-- CẦN CÁI NÀY

namespace FashionHub.Controllers
{
    public class ProductsController : Controller
    {
        private QL_SHOPQUANAO_PROEntities db = new QL_SHOPQUANAO_PROEntities();

        public ActionResult Index(string searchString, int? categoryId, List<int> colorIds, List<int> sizeIds, string priceRange, string sortBy, int page = 1)
        {
            IQueryable<SanPham> baseQuery = db.SanPhams
                .Where(p => p.TrangThai == true)
                .Include(p => p.BienTheSanPhams.Select(bt => bt.HinhAnh_BienThe.Select(ha => ha.HinhAnh)));

            if (!string.IsNullOrEmpty(searchString))
            {
                baseQuery = baseQuery.Where(p => p.TenSanPham.Contains(searchString));
            }
            if (categoryId.HasValue)
            {
                baseQuery = baseQuery.Where(p =>
                    p.IDDanhMuc == categoryId.Value ||
                    p.DanhMuc.IDDanhMucCha == categoryId.Value
                );
            }

            if (colorIds != null && colorIds.Any())
            {
                baseQuery = baseQuery.Where(p => p.BienTheSanPhams.Any(v => v.IDMauSac.HasValue && colorIds.Contains(v.IDMauSac.Value)));
            }

            if (sizeIds != null && sizeIds.Any())
            {
                baseQuery = baseQuery.Where(p => p.BienTheSanPhams.Any(v => v.IDKichThuoc.HasValue && sizeIds.Contains(v.IDKichThuoc.Value)));
            }

            switch (sortBy)
            {
                case "price_asc": baseQuery = baseQuery.OrderBy(p => p.Gia); break;
                case "price_desc": baseQuery = baseQuery.OrderByDescending(p => p.Gia); break;
                case "newest": default: baseQuery = baseQuery.OrderByDescending(p => p.IDSanPham); break;
            }

            int pageSize = 9;
            int totalItems = baseQuery.Count();
            var pagedQuery = baseQuery.Skip((page - 1) * pageSize).Take(pageSize);

            var productCards = pagedQuery.ToList().Select(p => new ProductCardViewModel
            {
                IDSanPham = p.IDSanPham,
                TenSanPham = p.TenSanPham,
                Gia = p.Gia,
                AnhChinhURL = p.BienTheSanPhams
                                .SelectMany(bt => bt.HinhAnh_BienThe)
                                .FirstOrDefault(habt => habt.LaAnhChinh == true)?
                                .HinhAnh.DuongDan ?? "/Content/images/placeholder.png",
                IsOutStock = !p.BienTheSanPhams.Any(bt => bt.SoLuongTon > 0),
                GiaKhuyenMai = p.GiaKhuyenMai,

                // --- ĐÃ BỔ SUNG ---
                NgayBatDauKM = p.NgayBatDauKM,
                NgayKetThucKM = p.NgayKetThucKM
            }).ToList();

            var viewModel = new ProductsViewModel
            {
                Products = productCards,
                Categories = db.DanhMucs.ToList(),
                Colors = db.MauSacs.ToList(),
                Sizes = db.KichThuocs.ToList(),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                SelectedCategoryId = categoryId,
                SelectedColorIds = colorIds ?? new List<int>(),
                SelectedSizeIds = sizeIds ?? new List<int>(),
                SelectedSortBy = sortBy,
                searchString = searchString
            };

            return View(viewModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        public ActionResult Details(int id)
        {
            var product = db.SanPhams
                .Where(p => p.IDSanPham == id && p.TrangThai == true)
                .Include(p => p.DanhMuc)
                .Include(p => p.ThuongHieu)
                .Include(p => p.BienTheSanPhams.Select(bt => bt.MauSac))
                .Include(p => p.BienTheSanPhams.Select(bt => bt.KichThuoc))
                .Include(p => p.BienTheSanPhams.Select(bt => bt.HinhAnh_BienThe.Select(ha => ha.HinhAnh)))
                .FirstOrDefault();

            if (product == null) return HttpNotFound();

            var allVariants = product.BienTheSanPhams.ToList();

            var viewModel = new ProductDetailViewModel
            {
                IDSanPham = product.IDSanPham,
                TenSanPham = product.TenSanPham,
                MoTa = product.MoTa,
                IDDanhMuc = product.DanhMuc.IDDanhMuc,
                TenDanhMuc = product.DanhMuc.TenDanhMuc,
                TenThuongHieu = product.ThuongHieu?.TenThuongHieu,
                Gia = product.Gia,
                GiaKhuyenMai = product.GiaKhuyenMai, // Đừng quên dòng này

                AvailableColors = allVariants.Where(v => v.MauSac != null).Select(v => v.MauSac).Distinct().ToList(),
                AvailableSizes = allVariants.Where(v => v.KichThuoc != null).Select(v => v.KichThuoc).Distinct().ToList(),
                IsOutStock = !allVariants.Any(v => v.SoLuongTon > 0),

                // --- ĐÃ CÓ (ĐÚNG) ---
                NgayBatDauKM = product.NgayBatDauKM,
                NgayKetThucKM = product.NgayKetThucKM,

                AllImages = product.BienTheSanPhams
                    .SelectMany(bt => bt.HinhAnh_BienThe.Select(ha => ha.HinhAnh))
                    .Distinct()
                    .ToList()
            };

            var variantsForJson = allVariants.Select(v => new ProductVariantViewModel
            {
                IDBienThe = v.IDBienThe,
                IDMauSac = v.IDMauSac,
                IDKichThuoc = v.IDKichThuoc,
                SoLuongTon = v.SoLuongTon,
                Sku = v.SKU,
                HinhAnhIDs = v.HinhAnh_BienThe.Select(ha => ha.IDHinhAnh).ToList()
            }).ToList();

            viewModel.VariantsJson = JsonConvert.SerializeObject(variantsForJson);

            viewModel.RelatedProducts = db.SanPhams
                .Where(p => p.IDDanhMuc == product.IDDanhMuc && p.IDSanPham != id && p.TrangThai == true)
                .Take(4)
                .ToList()
                .Select(p => new ProductCardViewModel
                {
                    IDSanPham = p.IDSanPham,
                    TenSanPham = p.TenSanPham,
                    Gia = p.Gia,
                    AnhChinhURL = p.BienTheSanPhams
                                    .SelectMany(bt => bt.HinhAnh_BienThe)
                                    .FirstOrDefault(habt => habt.LaAnhChinh == true)?
                                    .HinhAnh.DuongDan ?? "/Content/images/placeholder.png",
                    IsOutStock = !p.BienTheSanPhams.Any(bt => bt.SoLuongTon > 0),
                    GiaKhuyenMai = p.GiaKhuyenMai,

                    // --- ĐÃ SỬA LẠI CHO ĐÚNG BIẾN 'p' ---
                    NgayBatDauKM = p.NgayBatDauKM,
                    NgayKetThucKM = p.NgayKetThucKM
                }).ToList();

            return View(viewModel);
        }
        [HttpPost]
        public ActionResult SearchByImage(HttpPostedFileBase imageFile)
        {
            // Image search feature disabled to avoid dependency on local AI model or external services.
            // To re-enable, restore the original implementation in this method.
            return RedirectToAction("Index", new { error = "Tính năng tìm kiếm bằng hình ảnh đã bị tắt để ứng dụng hoạt động ổn định." });
        }

        // Hàm toán học tính độ giống nhau (Cosine Similarity)
        private double CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length) return 0;

            double dotProduct = 0.0;
            double magnitudeA = 0.0;
            double magnitudeB = 0.0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += Math.Pow(vectorA[i], 2);
                magnitudeB += Math.Pow(vectorB[i], 2);
            }

            if (magnitudeA == 0 || magnitudeB == 0) return 0;

            return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        }
    }
}