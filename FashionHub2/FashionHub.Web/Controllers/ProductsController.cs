using FashionHub.Web.Data;
using FashionHub.Web.ViewModels.Products;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace FashionHub.Web.Controllers;

public class ProductsController : Controller
{
    private readonly ApplicationDbContext db;

    public ProductsController(ApplicationDbContext context)
    {
        db = context;
    }

    public async Task<IActionResult> Index(string searchString, int? categoryId, List<int>? colorIds, List<int>? sizeIds, string? priceRange, string? sortBy, int page = 1)
    {
        IQueryable<Models.Generated.SanPham> baseQuery = db.SanPhams
            .Where(p => p.TrangThai == true)
            .Include(p => p.BienTheSanPhams).ThenInclude(bt => bt.HinhAnhBienThes).ThenInclude(habt => habt.IdhinhAnhNavigation);

        if (!string.IsNullOrEmpty(searchString))
        {
            baseQuery = baseQuery.Where(p => p.TenSanPham.Contains(searchString));
        }
        if (categoryId.HasValue)
        {
            baseQuery = baseQuery.Where(p =>
                p.IddanhMuc == categoryId.Value ||
                p.IddanhMucNavigation.IddanhMucCha == categoryId.Value
            );
        }

        if (colorIds != null && colorIds.Any())
        {
            baseQuery = baseQuery.Where(p => p.BienTheSanPhams.Any(v => v.IdmauSac.HasValue && colorIds.Contains(v.IdmauSac.Value)));
        }

        if (sizeIds != null && sizeIds.Any())
        {
            baseQuery = baseQuery.Where(p => p.BienTheSanPhams.Any(v => v.IdkichThuoc.HasValue && sizeIds.Contains(v.IdkichThuoc.Value)));
        }

        switch (sortBy)
        {
            case "price_asc": baseQuery = baseQuery.OrderBy(p => p.Gia); break;
            case "price_desc": baseQuery = baseQuery.OrderByDescending(p => p.Gia); break;
            case "newest": default: baseQuery = baseQuery.OrderByDescending(p => p.IdsanPham); break;
        }

        int pageSize = 9;
        int totalItems = await baseQuery.CountAsync();
        var pagedQuery = baseQuery.Skip((page - 1) * pageSize).Take(pageSize);

        var products = await pagedQuery.ToListAsync();
        var productCards = products.Select(p => new ProductCardViewModel
        {
            IDSanPham = p.IdsanPham,
            TenSanPham = p.TenSanPham,
            Gia = p.Gia,
            AnhChinhURL = p.BienTheSanPhams
                            .SelectMany(bt => bt.HinhAnhBienThes)
                            .FirstOrDefault(habt => habt.LaAnhChinh == true)?
                            .IdhinhAnhNavigation?.DuongDan ?? "/images/placeholder.png",
            IsOutStock = !p.BienTheSanPhams.Any(bt => bt.SoLuongTon > 0),
            GiaKhuyenMai = p.GiaKhuyenMai,
            NgayBatDauKM = p.NgayBatDauKm,
            NgayKetThucKM = p.NgayKetThucKm
        }).ToList();

        var viewModel = new ProductsViewModel
        {
            Products = productCards,
            Categories = await db.DanhMucs.ToListAsync(),
            Colors = await db.MauSacs.ToListAsync(),
            Sizes = await db.KichThuocs.ToListAsync(),
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

    public async Task<IActionResult> Details(int id)
    {
        var product = await db.SanPhams
            .Where(p => p.IdsanPham == id && p.TrangThai == true)
            .Include(p => p.IddanhMucNavigation)
            .Include(p => p.IdthuongHieuNavigation)
            .Include(p => p.BienTheSanPhams).ThenInclude(bt => bt.IdmauSacNavigation)
            .Include(p => p.BienTheSanPhams).ThenInclude(bt => bt.IdkichThuocNavigation)
            .Include(p => p.BienTheSanPhams).ThenInclude(bt => bt.HinhAnhBienThes).ThenInclude(habt => habt.IdhinhAnhNavigation)
            .FirstOrDefaultAsync();

        if (product == null) return NotFound();

        var allVariants = product.BienTheSanPhams.ToList();

        var viewModel = new ProductDetailViewModel
        {
            IDSanPham = product.IdsanPham,
            TenSanPham = product.TenSanPham,
            MoTa = product.MoTa,
            IDDanhMuc = product.IddanhMucNavigation.IddanhMuc,
            TenDanhMuc = product.IddanhMucNavigation.TenDanhMuc,
            TenThuongHieu = product.IdthuongHieuNavigation?.TenThuongHieu,
            Gia = product.Gia,
            GiaKhuyenMai = product.GiaKhuyenMai,

            AvailableColors = allVariants.Where(v => v.IdmauSacNavigation != null).Select(v => v.IdmauSacNavigation!).Distinct().ToList(),
            AvailableSizes = allVariants.Where(v => v.IdkichThuocNavigation != null).Select(v => v.IdkichThuocNavigation!).Distinct().ToList(),
            IsOutStock = !allVariants.Any(v => v.SoLuongTon > 0),

            NgayBatDauKM = product.NgayBatDauKm,
            NgayKetThucKM = product.NgayKetThucKm,

            AllImages = product.BienTheSanPhams
                .SelectMany(bt => bt.HinhAnhBienThes.Select(habt => habt.IdhinhAnhNavigation))
                .Distinct()
                .ToList()
        };

        var variantsForJson = allVariants.Select(v => new ProductVariantViewModel
        {
            IDBienThe = v.IdbienThe,
            IDMauSac = v.IdmauSac,
            IDKichThuoc = v.IdkichThuoc,
            SoLuongTon = v.SoLuongTon,
            Sku = v.Sku,
            HinhAnhIDs = v.HinhAnhBienThes.Select(ha => ha.IdhinhAnh).ToList()
        }).ToList();

        viewModel.VariantsJson = JsonConvert.SerializeObject(variantsForJson);

        var relatedProducts = await db.SanPhams
            .Where(p => p.IddanhMuc == product.IddanhMuc && p.IdsanPham != id && p.TrangThai == true)
            .Take(4)
            .Include(p => p.BienTheSanPhams).ThenInclude(bt => bt.HinhAnhBienThes).ThenInclude(habt => habt.IdhinhAnhNavigation)
            .ToListAsync();

        viewModel.RelatedProducts = relatedProducts.Select(p => new ProductCardViewModel
        {
            IDSanPham = p.IdsanPham,
            TenSanPham = p.TenSanPham,
            Gia = p.Gia,
            AnhChinhURL = p.BienTheSanPhams
                            .SelectMany(bt => bt.HinhAnhBienThes)
                            .FirstOrDefault(habt => habt.LaAnhChinh == true)?
                            .IdhinhAnhNavigation?.DuongDan ?? "/images/placeholder.png",
            IsOutStock = !p.BienTheSanPhams.Any(bt => bt.SoLuongTon > 0),
            GiaKhuyenMai = p.GiaKhuyenMai,
            NgayBatDauKM = p.NgayBatDauKm,
            NgayKetThucKM = p.NgayKetThucKm
        }).ToList();

        return View(viewModel);
    }

    [HttpPost]
    public IActionResult SearchByImage(IFormFile? imageFile)
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