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
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Index(string searchString)
        {
            // Chỉ lấy VaiTro = 2 (Khách hàng), không lấy Admin
            var usersQuery = _context.NguoiDungs
                .Include(u => u.IdvaiTroNavigation)
                .Where(u => u.IdvaiTro == 2);

            if (!string.IsNullOrEmpty(searchString))
            {
                usersQuery = usersQuery.Where(u =>
                    u.HoTen.Contains(searchString) ||
                    u.Email.Contains(searchString) ||
                    (u.SoDienThoai != null && u.SoDienThoai.Contains(searchString))
                );
            }

            var users = await usersQuery
                .OrderByDescending(u => u.NgayTao)
                .ToListAsync();

            var viewModels = users.Select(u => new UserViewModel
            {
                IdnguoiDung = u.IdnguoiDung,
                HoTen = u.HoTen,
                Email = u.Email,
                SoDienThoai = u.SoDienThoai,
                VaiTro = u.IdvaiTroNavigation.TenVaiTro,
                NgayTao = u.NgayTao,
                TrangThai = u.TrangThai,
                TotalOrders = _context.DonHangs.Count(d => d.IdnguoiDung == u.IdnguoiDung),
                TotalSpent = _context.DonHangs
                    .Where(d => d.IdnguoiDung == u.IdnguoiDung && d.IdtrangThai == 3)
                    .Sum(d => (decimal?)d.TongThanhToan) ?? 0
            }).ToList();

            ViewBag.SearchString = searchString;
            return View(viewModels);
        }

        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.NguoiDungs
                .Include(u => u.IdvaiTroNavigation)
                .FirstOrDefaultAsync(u => u.IdnguoiDung == id);

            if (user == null)
            {
                return NotFound();
            }

            // Lấy lịch sử đơn hàng của khách này
            var orderHistory = await _context.DonHangs
                .Where(d => d.IdnguoiDung == id)
                .Include(d => d.IdtrangThaiNavigation)
                .OrderByDescending(d => d.NgayTao)
                .ToListAsync();

            ViewBag.OrderHistory = orderHistory;

            // Tính tổng chi tiêu
            var totalSpent = await _context.DonHangs
                .Where(d => d.IdnguoiDung == id && d.IdtrangThai == 3)
                .SumAsync(d => (decimal?)d.TongThanhToan) ?? 0;

            ViewBag.TotalSpent = totalSpent;

            var viewModel = new UserViewModel
            {
                IdnguoiDung = user.IdnguoiDung,
                HoTen = user.HoTen,
                Email = user.Email,
                SoDienThoai = user.SoDienThoai,
                VaiTro = user.IdvaiTroNavigation.TenVaiTro,
                NgayTao = user.NgayTao,
                TrangThai = user.TrangThai,
                TotalOrders = orderHistory.Count,
                TotalSpent = totalSpent
            };

            return View(viewModel);
        }

        // POST: Admin/Users/ToggleStatus
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.NguoiDungs.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            // Đảo ngược trạng thái
            bool currentStatus = user.TrangThai;
            user.TrangThai = !currentStatus;

            await _context.SaveChangesAsync();

            return Json(new { success = true, newStatus = user.TrangThai });
        }
    }
}
