using FashionHub.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.ViewComponents
{
    public class MenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public MenuViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.DanhMucs
                .AsNoTracking()
                .Where(category => category.TrangThai && category.DeletedAt == null)
                .OrderBy(category => category.IddanhMucCha)
                .ThenBy(category => category.ThuTuHienThi)
                .ToListAsync();

            return View(categories);
        }
    }
}
