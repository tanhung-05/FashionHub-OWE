using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<ProductsController> logger)
        {
            _context = context;
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
                    TrangThai = p.TrangThai,
                    VariantCount = p.BienTheSanPhams.Count,
                    TotalStock = p.BienTheSanPhams.Sum(v => v.SoLuongTon),
                    MainImageUrl = p.BienTheSanPhams
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
            ViewBag.IDDanhMuc = await GetCategoryTreeAsync();
            ViewBag.IDThuongHieu = new SelectList(await _context.ThuongHieus.ToListAsync(), "IdthuongHieu", "TenThuongHieu");
            return View();
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductAdminViewModel model)
        {
            if (ModelState.IsValid)
            {
                var product = new SanPham
                {
                    TenSanPham = model.TenSanPham,
                    Slug = await CreateUniqueSlugAsync(model.TenSanPham),
                    MoTa = model.MoTa,
                    IddanhMuc = model.IDDanhMuc,
                    IdthuongHieu = model.IDThuongHieu,
                    Gia = model.Gia,
                    TrangThai = true,
                    NgayTao = DateTime.Now
                };

                _context.SanPhams.Add(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Tạo sản phẩm thành công! Hãy thêm biến thể.";
                return RedirectToAction(nameof(Edit), new { id = product.IdsanPham });
            }

            ViewBag.IDDanhMuc = await GetCategoryTreeAsync(model.IDDanhMuc);
            ViewBag.IDThuongHieu = new SelectList(await _context.ThuongHieus.ToListAsync(), "IdthuongHieu", "TenThuongHieu", model.IDThuongHieu);
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

            ViewBag.IDDanhMuc = await GetCategoryTreeAsync(product.IddanhMuc);
            ViewBag.IDThuongHieu = new SelectList(await _context.ThuongHieus.ToListAsync(), "IdthuongHieu", "TenThuongHieu", product.IdthuongHieu);
            ViewBag.Colors = new SelectList(await _context.MauSacs.ToListAsync(), "IdmauSac", "TenMau");
            ViewBag.Sizes = new SelectList(await _context.KichThuocs.ToListAsync(), "IdkichThuoc", "TenKichThuoc");

            // Load existing variants
            var variants = await _context.BienTheSanPhams
                .Where(v => v.IdsanPham == id && v.DeletedAt == null)
                .Include(v => v.IdmauSacNavigation)
                .Include(v => v.IdkichThuocNavigation)
                .Include(v => v.HinhAnhBienThes)
                    .ThenInclude(h => h.IdhinhAnhNavigation)
                .ToListAsync();

            ViewBag.ExistingVariants = variants.Select(v => new VariantDetailViewModel
            {
                IDBienThe = v.IdbienThe,
                SKU = v.Sku ?? "",
                TenMau = v.IdmauSacNavigation?.TenMau ?? "",
                TenKichThuoc = v.IdkichThuocNavigation?.TenKichThuoc ?? "",
                SoLuongTon = v.SoLuongTon,
                Images = v.HinhAnhBienThes.Select(h => new VariantImageViewModel
                {
                    IDHinhAnh = h.IdhinhAnh,
                    DuongDan = h.IdhinhAnhNavigation?.DuongDan ?? "",
                    LaAnhChinh = h.LaAnhChinh
                }).ToList()
            }).ToList();

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
                try
                {
                    var product = await _context.SanPhams
                        .FirstOrDefaultAsync(item => item.IdsanPham == id && item.DeletedAt == null);
                    if (product == null)
                    {
                        return NotFound();
                    }

                    product.TenSanPham = model.TenSanPham;
                    product.Slug = await CreateUniqueSlugAsync(model.TenSanPham, product.IdsanPham);
                    product.MoTa = model.MoTa;
                    product.IddanhMuc = model.IDDanhMuc;
                    product.IdthuongHieu = model.IDThuongHieu;
                    product.Gia = model.Gia;
                    product.TrangThai = model.TrangThai;
                    product.NgayCapNhat = DateTime.Now;

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction(nameof(Edit), new { id = model.IDSanPham });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ProductExistsAsync(model.IDSanPham))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            ViewBag.IDDanhMuc = await GetCategoryTreeAsync(model.IDDanhMuc);
            ViewBag.IDThuongHieu = new SelectList(await _context.ThuongHieus.ToListAsync(), "IdthuongHieu", "TenThuongHieu", model.IDThuongHieu);
            ViewBag.Colors = new SelectList(await _context.MauSacs.ToListAsync(), "IdmauSac", "TenMau");
            ViewBag.Sizes = new SelectList(await _context.KichThuocs.ToListAsync(), "IdkichThuoc", "TenKichThuoc");

            var variants = await _context.BienTheSanPhams
                .Where(v => v.IdsanPham == id && v.DeletedAt == null)
                .Include(v => v.IdmauSacNavigation)
                .Include(v => v.IdkichThuocNavigation)
                .Include(v => v.HinhAnhBienThes)
                    .ThenInclude(h => h.IdhinhAnhNavigation)
                .ToListAsync();

            ViewBag.ExistingVariants = variants.Select(v => new VariantDetailViewModel
            {
                IDBienThe = v.IdbienThe,
                SKU = v.Sku ?? "",
                TenMau = v.IdmauSacNavigation?.TenMau ?? "",
                TenKichThuoc = v.IdkichThuocNavigation?.TenKichThuoc ?? "",
                SoLuongTon = v.SoLuongTon,
                Images = v.HinhAnhBienThes.Select(h => new VariantImageViewModel
                {
                    IDHinhAnh = h.IdhinhAnh,
                    DuongDan = h.IdhinhAnhNavigation?.DuongDan ?? "",
                    LaAnhChinh = h.LaAnhChinh
                }).ToList()
            }).ToList();

            return View(model);
        }

        // POST: Admin/Products/AddVariant (AJAX)
        [HttpPost]
        public async Task<IActionResult> AddVariant([FromForm] ProductVariantAdminViewModel model)
        {
            try
            {
                // Check duplicate
                bool exists = await _context.BienTheSanPhams
                    .AnyAsync(x => x.IdsanPham == model.IDSanPham 
                        && x.IdmauSac == model.IDMauSac 
                        && x.IdkichThuoc == model.IDKichThuoc);

                if (exists)
                {
                    return Json(new { success = false, message = "Size hoặc màu này có trong danh sách biến thể của sản phẩm này rồi" });
                }

                if (model.SoLuongTon < 0)
                {
                    return Json(new { success = false, message = "Số lượng tồn không được nhỏ hơn 0" });
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

                // Handle image upload
                if (model.UploadImage != null && model.UploadImage.Length > 0)
                {
                    string fileName = Path.GetFileName(model.UploadImage.FileName);
                    string uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "products");
                    
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.UploadImage.CopyToAsync(fileStream);
                    }

                var hinhAnh = new HinhAnh 
                { 
                    DuongDan = $"/images/products/{uniqueFileName}",
                    NgayTao = DateTime.Now
                };
                _context.HinhAnhs.Add(hinhAnh);
                await _context.SaveChangesAsync();

                var link = new HinhAnhBienThe 
                { 
                    IdhinhAnh = hinhAnh.IdhinhAnh, 
                    IdbienThe = variant.IdbienThe, 
                    LaAnhChinh = true 
                };
                _context.HinhAnhBienThes.Add(link);
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Thêm biến thể thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding variant");
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
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
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
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
                        x.IdhinhAnh,
                        x.IdhinhAnhNavigation!.DuongDan,
                        x.LaAnhChinh
                    })
                    .ToListAsync();

                return Json(new { success = true, data = images });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting variant images");
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/Products/UploadImage (AJAX)
        [HttpPost]
        public async Task<IActionResult> UploadImage(int variantId, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn file." });
                }

                string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "products");
                
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                var hinhAnh = new HinhAnh { DuongDan = $"~/images/products/{fileName}" };
                _context.HinhAnhs.Add(hinhAnh);
                await _context.SaveChangesAsync();

                bool hasImage = await _context.HinhAnhBienThes.AnyAsync(x => x.IdbienThe == variantId);

                var link = new HinhAnhBienThe
                {
                    IdhinhAnh = hinhAnh.IdhinhAnh,
                    IdbienThe = variantId,
                    LaAnhChinh = !hasImage
                };
                _context.HinhAnhBienThes.Add(link);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Upload thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
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
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
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
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/Products/Delete (AJAX)
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _context.SanPhams.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                }

                var variants = await _context.BienTheSanPhams
                    .Where(v => v.IdsanPham == id && v.DeletedAt == null)
                    .ToListAsync();

                foreach (var variant in variants)
                {
                    variant.TrangThai = false;
                    variant.DeletedAt = DateTime.Now;
                    variant.NgayCapNhat = DateTime.Now;
                }

                product.TrangThai = false;
                product.DeletedAt = DateTime.Now;
                product.NgayCapNhat = DateTime.Now;

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
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

                var variant = await _context.BienTheSanPhams.FindAsync(variantId);
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
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
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
                var product = await _context.SanPhams.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                }

                if (discountPercent < 0 || discountPercent > 100)
                {
                    return Json(new { success = false, message = "% giảm giá không hợp lệ." });
                }

                if (discountPercent == 0)
                {
                    product.GiaKhuyenMai = null;
                    product.NgayBatDauKm = null;
                    product.NgayKetThucKm = null;
                }
                else
                {
                    decimal giamGia = product.Gia * discountPercent / 100;
                    product.GiaKhuyenMai = product.Gia - giamGia;
                    product.NgayBatDauKm = startDate ?? DateTime.Now;
                    product.NgayKetThucKm = endDate ?? DateTime.Now.AddDays(30);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật chương trình khuyến mãi thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying discount");
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
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
            var rootCategories = await _context.DanhMucs
                .Where(c => c.IddanhMucCha == null && c.DeletedAt == null && c.TrangThai)
                .ToListAsync();

            foreach (var cat in rootCategories)
            {
                items.Add(new SelectListItem
                {
                    Value = cat.IddanhMuc.ToString(),
                    Text = cat.TenDanhMuc?.ToUpper() ?? "",
                    Disabled = true
                });

                var childCategories = await _context.DanhMucs
                    .Where(c => c.IddanhMucCha == cat.IddanhMuc && c.DeletedAt == null && c.TrangThai)
                    .ToListAsync();

                foreach (var child in childCategories)
                {
                    items.Add(new SelectListItem
                    {
                        Value = child.IddanhMuc.ToString(),
                        Text = "|__ " + child.TenDanhMuc,
                        Selected = (selectedId == child.IddanhMuc)
                    });
                }
            }
            return items;
        }

        private async Task<bool> ProductExistsAsync(int id)
        {
            return await _context.SanPhams.AnyAsync(e => e.IdsanPham == id && e.DeletedAt == null);
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
