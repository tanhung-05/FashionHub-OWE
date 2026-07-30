using FashionHub.Web.Areas.Admin.ViewModels;
using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel();

            // Calculate stats
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);

            // Revenue excludes cancelled orders.
            viewModel.Stats.TotalRevenue = await _context.DonHangs
                .Where(d => d.IdtrangThai != OrderStatusIds.Cancelled)
                .SumAsync(d => d.TongThanhToan);

            // Total orders
            viewModel.Stats.TotalOrders = await _context.DonHangs.CountAsync();

            // Total products
            viewModel.Stats.TotalProducts = await _context.SanPhams.CountAsync();

            // Total users
            viewModel.Stats.TotalUsers = await _context.NguoiDungs.CountAsync();

            viewModel.Stats.PendingOrders = await _context.DonHangs
                .CountAsync(d => d.IdtrangThai == OrderStatusIds.Pending);

            viewModel.Stats.ProcessingOrders = await _context.DonHangs
                .CountAsync(d => d.IdtrangThai == OrderStatusIds.Confirmed
                    || d.IdtrangThai == OrderStatusIds.Shipping);

            viewModel.Stats.DeliveredOrders = await _context.DonHangs
                .CountAsync(d => d.IdtrangThai == OrderStatusIds.Completed);

            // Revenue growth (this month vs last month)
            var thisMonthRevenue = await _context.DonHangs
                .Where(d => d.IdtrangThai != OrderStatusIds.Cancelled && d.NgayTao >= startOfMonth)
                .SumAsync(d => d.TongThanhToan);

            var lastMonthRevenue = await _context.DonHangs
                .Where(d => d.IdtrangThai != OrderStatusIds.Cancelled &&
                           d.NgayTao >= startOfLastMonth && 
                           d.NgayTao < startOfMonth)
                .SumAsync(d => d.TongThanhToan);

            if (lastMonthRevenue > 0)
            {
                viewModel.Stats.RevenueGrowth = ((thisMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100;
            }

            // Order growth
            var thisMonthOrders = await _context.DonHangs
                .CountAsync(d => d.NgayTao >= startOfMonth);

            var lastMonthOrders = await _context.DonHangs
                .CountAsync(d => d.NgayTao >= startOfLastMonth && d.NgayTao < startOfMonth);

            if (lastMonthOrders > 0)
            {
                viewModel.Stats.OrderGrowth = ((thisMonthOrders - lastMonthOrders) * 100) / lastMonthOrders;
            }

            // Recent orders
            viewModel.RecentOrders = await _context.DonHangs
                .Include(d => d.IdnguoiDungNavigation)
                .Include(d => d.IdtrangThaiNavigation)
                .OrderByDescending(d => d.IddonHang)
                .Take(10)
                .Select(d => new RecentOrder
                {
                    OrderId = d.IddonHang,
                    CustomerName = d.IdnguoiDungNavigation != null 
                        ? d.IdnguoiDungNavigation.HoTen 
                        : d.TenNguoiNhan,
                    Total = d.TongThanhToan,
                    Status = d.IdtrangThaiNavigation != null
                        ? d.IdtrangThaiNavigation.TenTrangThai
                        : "N/A",
                    OrderDate = d.NgayTao
                })
                .ToListAsync();

            // Top products by sales (last 30 days) - simplified without images
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            viewModel.TopProducts = await _context.ChiTietDonHangs
                .Include(c => c.IdbienTheNavigation)
                    .ThenInclude(b => b!.IdsanPhamNavigation)
                .Where(c => c.IddonHangNavigation!.IdtrangThai != OrderStatusIds.Cancelled &&
                           c.IddonHangNavigation.NgayTao >= thirtyDaysAgo)
                .GroupBy(c => new
                {
                    ProductId = c.IdbienTheNavigation!.IdsanPhamNavigation!.IdsanPham,
                    ProductName = c.IdbienTheNavigation.IdsanPhamNavigation.TenSanPham
                })
                .Select(g => new TopProduct
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    ImageUrl = null, // Will be populated separately if needed
                    SoldCount = g.Sum(c => c.SoLuong),
                    Revenue = g.Sum(c => c.SoLuong * c.DonGia)
                })
                .OrderByDescending(p => p.SoldCount)
                .Take(5)
                .ToListAsync();

            // Monthly revenue for last 6 months
            var sixMonthsAgo = startOfMonth.AddMonths(-5);
            viewModel.MonthlyRevenues = await _context.DonHangs
                .Where(d => d.IdtrangThai != OrderStatusIds.Cancelled && d.NgayTao >= sixMonthsAgo)
                .GroupBy(d => new { d.NgayTao.Year, d.NgayTao.Month })
                .Select(g => new MonthlyRevenue
                {
                    Month = $"Tháng {g.Key.Month}/{g.Key.Year}",
                    Revenue = g.Sum(d => d.TongThanhToan),
                    OrderCount = g.Count()
                })
                .OrderBy(m => m.Month)
                .ToListAsync();

            return View(viewModel);
        }
    }
}
