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
    [AutoValidateAntiforgeryToken]
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
            viewModel.Stats.TotalProducts = await _context.SanPhams
                .CountAsync(product => product.DeletedAt == null);

            // Total users
            viewModel.Stats.TotalUsers = await _context.NguoiDungs
                .CountAsync(user => user.DeletedAt == null);

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
            else if (thisMonthRevenue > 0)
            {
                viewModel.Stats.RevenueGrowth = 100;
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
            else if (thisMonthOrders > 0)
            {
                viewModel.Stats.OrderGrowth = 100;
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
            var thirtyDaysAgo = now.AddDays(-30);
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

            var topProductIds = viewModel.TopProducts
                .Select(product => product.ProductId)
                .ToList();
            if (topProductIds.Count > 0)
            {
                var imageCandidates = await _context.HinhAnhBienThes
                    .AsNoTracking()
                    .Where(link =>
                        link.LaAnhChinh
                        && link.IdbienTheNavigation!.DeletedAt == null
                        && topProductIds.Contains(
                            link.IdbienTheNavigation.IdsanPham))
                    .Select(link => new
                    {
                        ProductId = link.IdbienTheNavigation!.IdsanPham,
                        ImageUrl = link.IdhinhAnhNavigation!.DuongDan
                    })
                    .ToListAsync();
                var imageByProduct = imageCandidates
                    .GroupBy(image => image.ProductId)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(image => image.ImageUrl).FirstOrDefault());

                foreach (var product in viewModel.TopProducts)
                {
                    if (imageByProduct.TryGetValue(product.ProductId, out var imageUrl))
                    {
                        product.ImageUrl = imageUrl;
                    }
                }
            }

            // Monthly revenue for last 6 months
            var sixMonthsAgo = startOfMonth.AddMonths(-5);
            var monthlyRevenueData = await _context.DonHangs
                .Where(d => d.IdtrangThai != OrderStatusIds.Cancelled && d.NgayTao >= sixMonthsAgo)
                .GroupBy(d => new { d.NgayTao.Year, d.NgayTao.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(d => d.TongThanhToan),
                    OrderCount = g.Count()
                })
                .OrderBy(item => item.Year)
                .ThenBy(item => item.Month)
                .ToListAsync();

            viewModel.MonthlyRevenues = monthlyRevenueData
                .Select(item => new MonthlyRevenue
                {
                    Month = $"Tháng {item.Month}/{item.Year}",
                    Revenue = item.Revenue,
                    OrderCount = item.OrderCount
                })
                .ToList();

            return View(viewModel);
        }
    }
}
