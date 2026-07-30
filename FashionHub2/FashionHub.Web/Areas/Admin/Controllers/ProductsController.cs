using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FashionHub.Web.Application.Admin;
using FashionHub.Web.Data;
using FashionHub.Web.Models.Generated;
using FashionHub.Web.Areas.Admin.ViewModels;
using FashionHub.Web.Domain;
using FashionHub.Web.Utilities;
using System.Security.Claims;
using System.Text;

namespace FashionHub.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [AutoValidateAntiforgeryToken]
    public class ProductsController : Controller
    {
        private const long MaxImageBytes = 5 * 1024 * 1024;
        private readonly ApplicationDbContext _context;
        private readonly IAdminProductService _productService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            ApplicationDbContext context,
            IAdminProductService productService,
            IWebHostEnvironment environment,
            ILogger<ProductsController> logger)
        {
            _context = context;
            _productService = productService;
            _environment = environment;
            _logger = logger;
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index(string? searchString, int? categoryId, int? brandId, int? status)
        {
            var query = _context.SanPhams
                .Include(p => p.IddanhMucNavigation)
                .Include(p => p.IdthuongHieuNavigation)
                .Include(p => p.BienTheSanPhams)
                    .ThenInclude(variant => variant.HinhAnhBienThes)
                        .ThenInclude(link => link.IdhinhAnhNavigation)
                .Where(p => p.DeletedAt == null)
                .AsQueryable();

            // Filter
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.TenSanPham.Contains(searchString));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.IddanhMuc == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                query = query.Where(p => p.IdthuongHieu == brandId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(p => p.TrangThai == (status.Value == 1));
            }

            query = query.OrderByDescending(p => p.IdsanPham);

            var products = await query.ToListAsync();

            var viewModel = new ProductListAdminViewModel
            {
                Products = products.Select(p => new ProductItemAdminViewModel
                {
                    IDSanPham = p.IdsanPham,
                    TenSanPham = p.TenSanPham ?? "",
                    TenDanhMuc = p.IddanhMucNavigation?.TenDanhMuc,
                    TenThuongHieu = p.IdthuongHieuNavigation?.TenThuongHieu,
                    Gia = p.Gia,
                    GiaKhuyenMai = p.GiaKhuyenMai,
                    NgayBatDauKm = p.NgayBatDauKm,
                    NgayKetThucKm = p.NgayKetThucKm,
                    TrangThai = p.TrangThai,
                    VariantCount = p.BienTheSanPhams.Count(v => v.DeletedAt == null),
                    TotalStock = p.BienTheSanPhams
                        .Where(v => v.DeletedAt == null)
                        .Sum(v => v.SoLuongTon),
                    MainImageUrl = p.BienTheSanPhams
                        .Where(v => v.DeletedAt == null)
                        .SelectMany(v => v.HinhAnhBienThes)
                        .Where(h => h.LaAnhChinh == true)
                        .Select(h => h.IdhinhAnhNavigation?.DuongDan)
                        .FirstOrDefault()
                }).ToList(),
                SearchString = searchString,
                CategoryId = categoryId,
                BrandId = brandId,
                Status = status
            };

            // Load dropdowns
            ViewBag.DanhMucs = await GetCategoryTreeAsync(categoryId);
            ViewBag.ThuongHieux = await _context.ThuongHieus.ToListAsync();

            return View(viewModel);
        }

        // GET: Admin/Products/Create
        public async Task<IActionResult> Create()
        {
            await LoadProductOptionsAsync();
            return View(new ProductAdminViewModel());
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductAdminViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _productService.CreateProductAsync(
                    new SaveAdminProductRequest
                    {
                        Name = model.TenSanPham,
                        Slug = await CreateUniqueSlugAsync(model.TenSanPham),
                        Description = model.MoTa,
                        Price = model.Gia,
                        CategoryId = model.IDDanhMuc,
                        BrandId = model.IDThuongHieu,
                        IsActive = true
                    });

                if (result.IsSuccess)
                {
                    TempData["Success"] = "Tạo sản phẩm thành công! Hãy thêm biến thể.";
                    return RedirectToAction(nameof(Edit), new { id = result.Value!.Id });
                }

                ModelState.AddModelError(string.Empty, result.Error!.Message);
            }

            await LoadProductOptionsAsync(model.IDDanhMuc, model.IDThuongHieu);
            return View(model);
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.SanPhams
                .FirstOrDefaultAsync(item => item.IdsanPham == id && item.DeletedAt == null);
            if (product == null)
            {
                return NotFound();
            }

            var model = new ProductAdminViewModel
            {
                IDSanPham = product.IdsanPham,
                TenSanPham = product.TenSanPham ?? "",
                MoTa = product.MoTa,
                IDDanhMuc = product.IddanhMuc ?? 0,
                IDThuongHieu = product.IdthuongHieu ?? 0,
                Gia = product.Gia,
                TrangThai = product.TrangThai
            };

            await LoadProductOptionsAsync(product.IddanhMuc, product.IdthuongHieu, includeVariants: true);
            model.Variants = await LoadVariantsAsync(product.IdsanPham);

            return View(model);
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductAdminViewModel model)
        {
            if (id != model.IDSanPham)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existing = await _productService.GetProductAsync(id);
                if (!existing.IsSuccess)
                {
                    return NotFound();
                }

                var current = existing.Value!;
                var result = await _productService.UpdateProductAsync(
                    id,
                    new SaveAdminProductRequest
                    {
                        Name = model.TenSanPham,
                        Slug = await CreateUniqueSlugAsync(model.TenSanPham, id),
                        Description = model.MoTa,
                        Price = model.Gia,
                        SalePrice = current.SalePrice,
                        SaleStart = current.SaleStart,
                        SaleEnd = current.SaleEnd,
                        CategoryId = model.IDDanhMuc,
                        BrandId = model.IDThuongHieu,
                        IsActive = model.TrangThai
                    });

                if (result.IsSuccess)
                {
                    TempData["Success"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction(nameof(Edit), new { id = model.IDSanPham });
                }

                ModelState.AddModelError(string.Empty, result.Error!.Message);
            }

            await LoadProductOptionsAsync(model.IDDanhMuc, model.IDThuongHieu, includeVariants: true);
            model.Variants = await LoadVariantsAsync(id);

            return View(model);
        }

        // POST: Admin/Products/AddVariant (AJAX)
        [HttpPost]
        public async Task<IActionResult> AddVariant([FromForm] ProductVariantAdminViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var message = ModelState.Values
                        .SelectMany(value => value.Errors)
                        .Select(error => error.ErrorMessage)
                        .FirstOrDefault() ?? "Dữ liệu biến thể không hợp lệ.";
                    return Json(new { success = false, message });
                }

                var productExists = await _context.SanPhams.AnyAsync(product =>
                    product.IdsanPham == model.IDSanPham
                    && product.DeletedAt == null);
                if (!productExists)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                }

                var attributesExist =
                    await _context.MauSacs.AnyAsync(color => color.IdmauSac == model.IDMauSac)
                    && await _context.KichThuocs.AnyAsync(size => size.IdkichThuoc == model.IDKichThuoc);
                if (!attributesExist)
                {
                    return Json(new { success = false, message = "Màu sắc hoặc kích thước không hợp lệ." });
                }

                // Check duplicate
                bool exists = await _context.BienTheSanPhams
                    .AnyAsync(x => x.IdsanPham == model.IDSanPham 
                        && x.IdmauSac == model.IDMauSac 
                        && x.IdkichThuoc == model.IDKichThuoc
                        && x.DeletedAt == null);

                if (exists)
                {
                    return Json(new { success = false, message = "Size hoặc màu này có trong danh sách biến thể của sản phẩm này rồi" });
                }

                if (model.UploadImage is not null)
                {
                    var imageError = await ValidateImageAsync(model.UploadImage);
                    if (imageError is not null)
                    {
                        return Json(new { success = false, message = imageError });
                    }
                }

                string sku = $"SP{model.IDSanPham}-C{model.IDMauSac}-S{model.IDKichThuoc}-{DateTime.Now.Ticks.ToString()[^10..]}";

                var variant = new BienTheSanPham
                {
                    IdsanPham = model.IDSanPham,
                    IdmauSac = model.IDMauSac,
                    IdkichThuoc = model.IDKichThuoc,
                    SoLuongTon = model.SoLuongTon,
                    SoLuongCanhBao = 10,
                    Sku = sku,
                    TrangThai = true,
                    NgayTao = DateTime.Now
                };

                _context.BienTheSanPhams.Add(variant);
                await _context.SaveChangesAsync();

                if (model.SoLuongTon > 0)
                {
                    _context.LichSuTonKhos.Add(new LichSuTonKho
                    {
                        IdbienThe = variant.IdbienThe,
                        IdnguoiThucHien = GetCurrentUserId(),
                        LoaiThayDoi = InventoryChangeTypes.ManualImport,
                        SoLuongThayDoi = model.SoLuongTon,
                        TonTruoc = 0,
                        TonSau = model.SoLuongTon,
                        GhiChu = "Tồn kho ban đầu khi tạo biến thể",
                        NgayTao = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                }

                // Handle image upload
                if (model.UploadImage != null && model.UploadImage.Length > 0)
                {
                    var imagePath = await SaveImageFileAsync(model.UploadImage);
                    var hinhAnh = new HinhAnh
                    {
                        DuongDan = imagePath,
                        NgayTao = DateTime.Now
                    };
                    _context.HinhAnhs.Add(hinhAnh);
                    await _context.SaveChangesAsync();

                    _context.HinhAnhBienThes.Add(new HinhAnhBienThe
                    {
                        IdhinhAnh = hinhAnh.IdhinhAnh,
                        IdbienThe = variant.IdbienThe,
                        LaAnhChinh = true
                    });
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Thêm biến thể thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding variant");
                return Json(new { success = false, message = "Không thể thêm biến thể. Vui lòng thử lại." });
            }
        }

        // POST: Admin/Products/DeleteVariant (AJAX)
        [HttpPost]
        public async Task<IActionResult> DeleteVariant(int id)
        {
            try
            {
                var variant = await _context.BienTheSanPhams.FindAsync(id);
                if (variant == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy biến thể." });
                }

                variant.TrangThai = false;
                variant.DeletedAt = DateTime.Now;
                variant.NgayCapNhat = DateTime.Now;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting variant");
                return Json(new { success = false, message = "Không thể xóa biến thể. Vui lòng thử lại." });
            }
        }

        // GET: Admin/Products/GetVariantImages (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetVariantImages(int variantId)
        {
            try
            {
                var images = await _context.HinhAnhBienThes
                    .Where(x => x.IdbienThe == variantId)
                    .Include(x => x.IdhinhAnhNavigation)
                    .Select(x => new
                    {
                        id = x.IdhinhAnh,
                        url = x.IdhinhAnhNavigation!.DuongDan,
                        isMain = x.LaAnhChinh
                    })
                    .ToListAsync();

                return Json(new { success = true, data = images });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting variant images");
                return Json(new { success = false, message = "Không thể tải danh sách ảnh." });
            }
        }

        // POST: Admin/Products/UploadImage (AJAX)
        [HttpPost]
        public async Task<IActionResult> UploadImage(int variantId, IFormFile? file)
        {
            return file is null
                ? Json(new { success = false, message = "Vui lòng chọn ảnh." })
                : await UploadImages(variantId, new List<IFormFile> { file });
        }

        // POST: Admin/Products/UploadImages (AJAX)
        [HttpPost]
        public async Task<IActionResult> UploadImages(int variantId, List<IFormFile>? images)
        {
            try
            {
                var variant = await _context.BienTheSanPhams
                    .FirstOrDefaultAsync(item =>
                        item.IdbienThe == variantId
                        && item.DeletedAt == null);
                if (variant is null)
                {
                    return Json(new { success = false, message = "Không tìm thấy biến thể." });
                }

                if (images is null || images.Count is < 1 or > 10)
                {
                    return Json(new { success = false, message = "Vui lòng chọn từ 1 đến 10 ảnh." });
                }

                foreach (var image in images)
                {
                    var imageError = await ValidateImageAsync(image);
                    if (imageError is not null)
                    {
                        return Json(new { success = false, message = imageError });
                    }
                }

                bool hasImage = await _context.HinhAnhBienThes.AnyAsync(x => x.IdbienThe == variantId);
                foreach (var image in images)
                {
                    var hinhAnh = new HinhAnh
                    {
                        DuongDan = await SaveImageFileAsync(image),
                        NgayTao = DateTime.Now
                    };
                    _context.HinhAnhs.Add(hinhAnh);
                    await _context.SaveChangesAsync();

                    _context.HinhAnhBienThes.Add(new HinhAnhBienThe
                    {
                        IdhinhAnh = hinhAnh.IdhinhAnh,
                        IdbienThe = variantId,
                        LaAnhChinh = !hasImage
                    });
                    hasImage = true;
                }
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Đã tải lên {images.Count} ảnh." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading images for variant {VariantId}", variantId);
                return Json(new { success = false, message = "Không thể tải ảnh lên. Vui lòng thử lại." });
            }
        }

        // POST: Admin/Products/SetMainImage (AJAX)
        [HttpPost]
        public async Task<IActionResult> SetMainImage(int variantId, int imageId)
        {
            try
            {
                var links = await _context.HinhAnhBienThes
                    .Where(x => x.IdbienThe == variantId)
                    .ToListAsync();

                foreach (var item in links)
                {
                    item.LaAnhChinh = false;
                }

                var target = links.FirstOrDefault(x => x.IdhinhAnh == imageId);
                if (target != null)
                {
                    target.LaAnhChinh = true;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Không tìm thấy ảnh." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting main image");
                return Json(new { success = false, message = "Không thể đặt ảnh chính." });
            }
        }

        // POST: Admin/Products/DeleteImage (AJAX)
        [HttpPost]
        public async Task<IActionResult> DeleteImage(int variantId, int imageId)
        {
            try
            {
                var link = await _context.HinhAnhBienThes
                    .FirstOrDefaultAsync(x => x.IdbienThe == variantId && x.IdhinhAnh == imageId);

                if (link == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy ảnh." });
                }

                bool wasMain = link.LaAnhChinh;

                _context.HinhAnhBienThes.Remove(link);
                await _context.SaveChangesAsync();

                if (wasMain)
                {
                    var nextImage = await _context.HinhAnhBienThes
                        .FirstOrDefaultAsync(x => x.IdbienThe == variantId);
                    if (nextImage != null)
                    {
                        nextImage.LaAnhChinh = true;
                        await _context.SaveChangesAsync();
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image");
                return Json(new { success = false, message = "Không thể xóa ảnh." });
            }
        }

        // POST: Admin/Products/Delete (AJAX)
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _productService.DeleteProductAsync(id);
                return result.IsSuccess
                    ? Json(new { success = true, message = "Xóa sản phẩm thành công." })
                    : Json(new { success = false, message = result.Error!.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                return Json(new { success = false, message = "Không thể xóa sản phẩm. Vui lòng thử lại." });
            }
        }

        // POST: Admin/Products/ImportStock (AJAX)
        [HttpPost]
        public async Task<IActionResult> ImportStock(int variantId, int quantityToAdd)
        {
            try
            {
                if (quantityToAdd <= 0)
                {
                    return Json(new { success = false, message = "Số lượng nhập phải lớn hơn 0." });
                }

                var variant = await _context.BienTheSanPhams
                    .FirstOrDefaultAsync(item =>
                        item.IdbienThe == variantId
                        && item.DeletedAt == null);
                if (variant == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy biến thể." });
                }

                var previousStock = variant.SoLuongTon;
                variant.SoLuongTon += quantityToAdd;
                variant.NgayCapNhat = DateTime.Now;
                _context.LichSuTonKhos.Add(new LichSuTonKho
                {
                    IdbienThe = variant.IdbienThe,
                    IdnguoiThucHien = GetCurrentUserId(),
                    LoaiThayDoi = InventoryChangeTypes.ManualImport,
                    SoLuongThayDoi = quantityToAdd,
                    TonTruoc = previousStock,
                    TonSau = variant.SoLuongTon,
                    GhiChu = "Admin nhập kho",
                    NgayTao = DateTime.Now
                });
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Nhập kho thành công!", newStock = variant.SoLuongTon });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing stock");
                return Json(new { success = false, message = "Không thể nhập kho. Vui lòng thử lại." });
            }
        }

        private int? GetCurrentUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claimValue, out var userId) ? userId : null;
        }

        // POST: Admin/Products/ApplyDiscount (AJAX)
        [HttpPost]
        public async Task<IActionResult> ApplyDiscount(int id, int discountPercent, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var existing = await _productService.GetProductAsync(id);
                if (!existing.IsSuccess)
                {
                    return Json(new { success = false, message = existing.Error!.Message });
                }

                if (discountPercent < 0 || discountPercent > 100)
                {
                    return Json(new { success = false, message = "% giảm giá không hợp lệ." });
                }

                var product = existing.Value!;
                decimal? salePrice = null;
                DateTime? saleStart = null;
                DateTime? saleEnd = null;

                if (discountPercent > 0)
                {
                    saleStart = startDate ?? DateTime.Now;
                    saleEnd = endDate ?? DateTime.Now.AddDays(30);
                    if (saleEnd <= saleStart)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Ngày kết thúc phải sau ngày bắt đầu."
                        });
                    }

                    salePrice = product.Price - (product.Price * discountPercent / 100);
                }

                var result = await _productService.UpdateProductAsync(
                    id,
                    new SaveAdminProductRequest
                    {
                        Name = product.Name,
                        Slug = product.Slug,
                        Description = product.Description,
                        Price = product.Price,
                        SalePrice = salePrice,
                        SaleStart = saleStart,
                        SaleEnd = saleEnd,
                        CategoryId = product.CategoryId,
                        BrandId = product.BrandId,
                        IsActive = product.IsActive
                    });
                if (!result.IsSuccess)
                {
                    return Json(new { success = false, message = result.Error!.Message });
                }

                return Json(new { success = true, message = "Cập nhật chương trình khuyến mãi thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying discount");
                return Json(new { success = false, message = "Không thể cập nhật khuyến mãi. Vui lòng thử lại." });
            }
        }

        // GET: Admin/Products/Export
        public async Task<IActionResult> Export()
        {
            var data = await _context.BienTheSanPhams
                .Where(variant => variant.DeletedAt == null
                    && variant.IdsanPhamNavigation.DeletedAt == null)
                .Include(b => b.IdsanPhamNavigation)
                    .ThenInclude(s => s!.IddanhMucNavigation)
                .Include(b => b.IdsanPhamNavigation)
                    .ThenInclude(s => s!.IdthuongHieuNavigation)
                .Include(b => b.IdmauSacNavigation)
                .Include(b => b.IdkichThuocNavigation)
                .OrderBy(b => b.IdsanPham)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Mã SP,Tên Sản Phẩm,Danh Mục,Thương Hiệu,SKU,Màu Sắc,Kích Thước,Giá Bán,Giá KM,Tồn Kho,Trạng Thái");

            foreach (var item in data)
            {
                string tenSP = item.IdsanPhamNavigation?.TenSanPham?.Replace(",", " ") ?? "";
                string danhMuc = item.IdsanPhamNavigation?.IddanhMucNavigation?.TenDanhMuc ?? "";
                string thuongHieu = item.IdsanPhamNavigation?.IdthuongHieuNavigation?.TenThuongHieu ?? "";
                string trangThai = item.IdsanPhamNavigation?.TrangThai == true ? "Đang bán" : "Ngừng bán";

                sb.AppendLine(
                    $"{item.IdsanPham},{tenSP},{danhMuc},{thuongHieu},{item.Sku}," +
                    $"{item.IdmauSacNavigation?.TenMau},{item.IdkichThuocNavigation?.TenKichThuoc}," +
                    $"{item.Gia},{item.IdsanPhamNavigation?.GiaKhuyenMai},{item.SoLuongTon},{trangThai}");
            }

            byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
            byte[] bom = Encoding.UTF8.GetPreamble();
            byte[] result = new byte[bom.Length + buffer.Length];
            Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
            Buffer.BlockCopy(buffer, 0, result, bom.Length, buffer.Length);

            return File(result, "text/csv", $"DanhSachSanPham_{DateTime.Now:yyyyMMdd}.csv");
        }

        // Helper: Get category tree for dropdown
        private async Task<List<SelectListItem>> GetCategoryTreeAsync(int? selectedId = null)
        {
            var items = new List<SelectListItem>();
            var categories = await _context.DanhMucs
                .AsNoTracking()
                .Where(category => category.DeletedAt == null && category.TrangThai)
                .OrderBy(category => category.TenDanhMuc)
                .ToListAsync();

            foreach (var category in categories.Where(item => item.IddanhMucCha == null))
            {
                items.Add(new SelectListItem
                {
                    Value = category.IddanhMuc.ToString(),
                    Text = category.TenDanhMuc,
                    Selected = selectedId == category.IddanhMuc
                });

                foreach (var child in categories.Where(item =>
                    item.IddanhMucCha == category.IddanhMuc))
                {
                    items.Add(new SelectListItem
                    {
                        Value = child.IddanhMuc.ToString(),
                        Text = $"— {child.TenDanhMuc}",
                        Selected = selectedId == child.IddanhMuc
                    });
                }
            }
            return items;
        }

        private async Task LoadProductOptionsAsync(
            int? selectedCategoryId = null,
            int? selectedBrandId = null,
            bool includeVariants = false)
        {
            ViewBag.DanhMucs = await GetCategoryTreeAsync(selectedCategoryId);
            ViewBag.ThuongHieux = new SelectList(
                await _context.ThuongHieus
                    .Where(brand => brand.DeletedAt == null)
                    .OrderBy(brand => brand.TenThuongHieu)
                    .ToListAsync(),
                nameof(ThuongHieu.IdthuongHieu),
                nameof(ThuongHieu.TenThuongHieu),
                selectedBrandId);

            if (includeVariants)
            {
                ViewBag.Colors = await _context.MauSacs
                    .OrderBy(color => color.TenMau)
                    .ToListAsync();
                ViewBag.Sizes = await _context.KichThuocs
                    .OrderBy(size => size.TenKichThuoc)
                    .ToListAsync();
            }
        }

        private async Task<List<VariantDetailViewModel>> LoadVariantsAsync(int productId)
        {
            var variants = await _context.BienTheSanPhams
                .AsNoTracking()
                .Where(variant =>
                    variant.IdsanPham == productId
                    && variant.DeletedAt == null)
                .Include(variant => variant.IdmauSacNavigation)
                .Include(variant => variant.IdkichThuocNavigation)
                .Include(variant => variant.HinhAnhBienThes)
                    .ThenInclude(link => link.IdhinhAnhNavigation)
                .OrderBy(variant => variant.IdmauSacNavigation!.TenMau)
                .ThenBy(variant => variant.IdkichThuocNavigation!.TenKichThuoc)
                .ToListAsync();

            return variants.Select(variant => new VariantDetailViewModel
            {
                IDBienThe = variant.IdbienThe,
                SKU = variant.Sku ?? string.Empty,
                TenMau = variant.IdmauSacNavigation?.TenMau ?? string.Empty,
                TenKichThuoc = variant.IdkichThuocNavigation?.TenKichThuoc ?? string.Empty,
                SoLuongTon = variant.SoLuongTon,
                Images = variant.HinhAnhBienThes.Select(link => new VariantImageViewModel
                {
                    IDHinhAnh = link.IdhinhAnh,
                    DuongDan = link.IdhinhAnhNavigation?.DuongDan ?? string.Empty,
                    LaAnhChinh = link.LaAnhChinh
                }).ToList()
            }).ToList();
        }

        private static async Task<string?> ValidateImageAsync(IFormFile file)
        {
            if (file.Length == 0)
            {
                return "Tệp ảnh đang trống.";
            }

            if (file.Length > MaxImageBytes)
            {
                return "Mỗi ảnh không được vượt quá 5 MB.";
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not ".jpg" and not ".jpeg" and not ".png" and not ".gif" and not ".webp")
            {
                return "Chỉ chấp nhận ảnh JPG, PNG, GIF hoặc WebP.";
            }

            var header = new byte[12];
            await using var stream = file.OpenReadStream();
            var bytesRead = await stream.ReadAsync(header);
            var isJpeg = bytesRead >= 3
                && header[0] == 0xFF
                && header[1] == 0xD8
                && header[2] == 0xFF;
            var isPng = bytesRead >= 8
                && header.AsSpan(0, 8).SequenceEqual(
                    new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
            var isGif = bytesRead >= 6
                && Encoding.ASCII.GetString(header, 0, 6) is "GIF87a" or "GIF89a";
            var isWebP = bytesRead >= 12
                && Encoding.ASCII.GetString(header, 0, 4) == "RIFF"
                && Encoding.ASCII.GetString(header, 8, 4) == "WEBP";

            return isJpeg || isPng || isGif || isWebP
                ? null
                : "Nội dung tệp không phải là định dạng ảnh hợp lệ.";
        }

        private async Task<string> SaveImageFileAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var webRoot = _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "images", "products");
            Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);
            await using var fileStream = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
            await file.CopyToAsync(fileStream);
            return $"/images/products/{fileName}";
        }

        private async Task<string> CreateUniqueSlugAsync(string name, int? excludeId = null)
        {
            var baseSlug = SlugGenerator.Generate(name);
            var slug = baseSlug;
            var suffix = 2;

            while (await _context.SanPhams.AnyAsync(product =>
                       product.Slug == slug
                       && product.DeletedAt == null
                       && (!excludeId.HasValue || product.IdsanPham != excludeId.Value)))
            {
                slug = $"{baseSlug}-{suffix++}";
            }

            return slug;
        }
    }
}
