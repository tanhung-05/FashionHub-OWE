using FashionHub.Web.Areas.Admin.ViewModels;
using FashionHub.Web.Data;
using FashionHub.Web.Models.Generated;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchTerm)
        {
            var query = _context.DanhMucs
                .Include(d => d.IddanhMucChaNavigation)
                .Include(d => d.SanPhams)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(d => d.TenDanhMuc.Contains(searchTerm));
            }

            var categories = await query
                .OrderBy(d => d.IddanhMucCha ?? 0)
                .ThenBy(d => d.TenDanhMuc)
                .ToListAsync();

            var viewModel = new CategoryListViewModel
            {
                SearchTerm = searchTerm,
                Categories = categories.Select(MapToViewModel).ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.ParentCategories = await GetParentCategoriesAsync();
            return View(new CategoryViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if category name already exists
                var exists = await _context.DanhMucs
                    .AnyAsync(d => d.TenDanhMuc == model.Name && 
                                   d.IddanhMucCha == model.ParentCategoryId);

                if (exists)
                {
                    ModelState.AddModelError("Name", "Danh mục này đã tồn tại.");
                }
                else
                {
                    var category = new DanhMuc
                    {
                        TenDanhMuc = model.Name,
                        IddanhMucCha = model.ParentCategoryId
                    };

                    _context.DanhMucs.Add(category);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thêm danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.ParentCategories = await GetParentCategoriesAsync();
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.DanhMucs
                .Include(d => d.IddanhMucChaNavigation)
                .FirstOrDefaultAsync(d => d.IddanhMuc == id);

            if (category == null)
            {
                return NotFound();
            }

            var viewModel = MapToViewModel(category);
            ViewBag.ParentCategories = await GetParentCategoriesAsync(id);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                var category = await _context.DanhMucs.FindAsync(id);
                if (category == null)
                {
                    return NotFound();
                }

                // Check if trying to set itself as parent
                if (model.ParentCategoryId == id)
                {
                    ModelState.AddModelError("ParentCategoryId", "Không thể chọn chính nó làm danh mục cha.");
                }
                // Check if name already exists (excluding current category)
                else if (await _context.DanhMucs.AnyAsync(d => 
                    d.TenDanhMuc == model.Name && 
                    d.IddanhMucCha == model.ParentCategoryId && 
                    d.IddanhMuc != id))
                {
                    ModelState.AddModelError("Name", "Danh mục này đã tồn tại.");
                }
                else
                {
                    category.TenDanhMuc = model.Name;
                    category.IddanhMucCha = model.ParentCategoryId;

                    _context.Update(category);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.ParentCategories = await GetParentCategoriesAsync(id);
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.DanhMucs
                .Include(d => d.IddanhMucChaNavigation)
                .Include(d => d.SanPhams)
                .Include(d => d.InverseIddanhMucChaNavigation)
                .FirstOrDefaultAsync(d => d.IddanhMuc == id);

            if (category == null)
            {
                return NotFound();
            }

            var viewModel = MapToViewModel(category);
            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.DanhMucs
                .Include(d => d.SanPhams)
                .Include(d => d.InverseIddanhMucChaNavigation)
                .FirstOrDefaultAsync(d => d.IddanhMuc == id);

            if (category == null)
            {
                return NotFound();
            }

            // Check if category has products
            if (category.SanPhams.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa danh mục đang có sản phẩm. Vui lòng xóa hoặc chuyển sản phẩm sang danh mục khác.";
                return RedirectToAction(nameof(Index));
            }

            // Check if category has subcategories
            if (category.InverseIddanhMucChaNavigation.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa danh mục đang có danh mục con. Vui lòng xóa danh mục con trước.";
                return RedirectToAction(nameof(Index));
            }

            _context.DanhMucs.Remove(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa danh mục thành công!";
            return RedirectToAction(nameof(Index));
        }

        private CategoryViewModel MapToViewModel(DanhMuc category)
        {
            return new CategoryViewModel
            {
                Id = category.IddanhMuc,
                Name = category.TenDanhMuc,
                ParentCategoryId = category.IddanhMucCha,
                ParentCategoryName = category.IddanhMucChaNavigation?.TenDanhMuc,
                ProductCount = category.SanPhams?.Count ?? 0,
                SubCategories = category.InverseIddanhMucChaNavigation?
                    .Select(MapToViewModel)
                    .ToList() ?? new List<CategoryViewModel>()
            };
        }

        private async Task<List<CategoryViewModel>> GetParentCategoriesAsync(int? excludeId = null)
        {
            var query = _context.DanhMucs.AsQueryable();

            if (excludeId.HasValue)
            {
                query = query.Where(d => d.IddanhMuc != excludeId.Value);
            }

            var categories = await query
                .Where(d => d.IddanhMucCha == null) // Only root categories
                .OrderBy(d => d.TenDanhMuc)
                .Select(d => new CategoryViewModel
                {
                    Id = d.IddanhMuc,
                    Name = d.TenDanhMuc
                })
                .ToListAsync();

            return categories;
        }
    }
}