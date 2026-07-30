using FashionHub.Web.Areas.Admin.ViewModels;
using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using FashionHub.Web.Models.Generated;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [AutoValidateAntiforgeryToken]
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
            var usersQuery = _context.NguoiDungs
                .AsNoTracking()
                .Where(user => user.IdvaiTro == 2 && user.DeletedAt == null);

            if (!string.IsNullOrEmpty(searchString))
            {
                usersQuery = usersQuery.Where(u =>
                    u.HoTen.Contains(searchString) ||
                    u.Email.Contains(searchString) ||
                    (u.SoDienThoai != null && u.SoDienThoai.Contains(searchString))
                );
            }

            var viewModels = await usersQuery
                .OrderByDescending(user => user.NgayTao)
                .Select(user => new UserViewModel
                {
                    IdnguoiDung = user.IdnguoiDung,
                    HoTen = user.HoTen,
                    Email = user.Email,
                    SoDienThoai = user.SoDienThoai,
                    VaiTro = user.IdvaiTroNavigation.TenVaiTro,
                    NgayTao = user.NgayTao,
                    TrangThai = user.TrangThai,
                    TotalOrders = _context.DonHangs.Count(order =>
                        order.IdnguoiDung == user.IdnguoiDung),
                    TotalSpent = _context.DonHangs
                        .Where(order =>
                            order.IdnguoiDung == user.IdnguoiDung
                            && order.IdtrangThai == OrderStatusIds.Completed)
                        .Sum(order => (decimal?)order.TongThanhToan) ?? 0
                })
                .ToListAsync();

            ViewBag.SearchString = searchString;
            return View(viewModels);
        }

        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.NguoiDungs
                .Include(u => u.IdvaiTroNavigation)
                .FirstOrDefaultAsync(u =>
                    u.IdnguoiDung == id
                    && u.IdvaiTro == 2
                    && u.DeletedAt == null);

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
                .Where(d =>
                    d.IdnguoiDung == id
                    && d.IdtrangThai == OrderStatusIds.Completed)
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
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(item =>
                item.IdnguoiDung == id
                && item.IdvaiTro == 2
                && item.DeletedAt == null);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            user.TrangThai = !user.TrangThai;
            user.SecurityStamp = Guid.NewGuid();

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                newStatus = user.TrangThai,
                message = user.TrangThai
                    ? "Đã mở khóa tài khoản."
                    : "Đã khóa tài khoản và thu hồi phiên đăng nhập."
            });
        }
    }
}
