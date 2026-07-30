using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FashionHub.Web.Data;
using FashionHub.Web.Areas.Admin.ViewModels;
using FashionHub.Web.Domain;

namespace FashionHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> SalesReport(DateTime? startDate, DateTime? endDate, string period = "daily")
    {
        // Mặc định: 30 ngày gần nhất
        var end = endDate ?? DateTime.Now.Date;
        var start = startDate ?? end.AddDays(-30);

        var orders = await _context.DonHangs
            .Where(o => o.IdtrangThai != OrderStatusIds.Cancelled
                && o.NgayTao >= start
                && o.NgayTao <= end.AddDays(1).AddSeconds(-1))
            .ToListAsync();

        var report = new SalesReportViewModel
        {
            StartDate = start,
            EndDate = end,
            Period = period,
            TotalOrders = orders.Count,
            TotalRevenue = orders.Sum(o => o.TongThanhToan),
            TotalDiscount = orders.Sum(o => o.TienGiamGia),
            TotalShipping = orders.Sum(o => o.PhiVanChuyen),
            AverageOrderValue = orders.Any() ? orders.Average(o => o.TongThanhToan) : 0m
        };

        // Nhóm theo thời gian
        if (period == "daily")
        {
            report.ChartLabels = Enumerable.Range(0, (int)(end - start).TotalDays + 1)
                .Select(i => start.AddDays(i).ToString("dd/MM"))
                .ToList();

            report.ChartData = Enumerable.Range(0, (int)(end - start).TotalDays + 1)
                .Select(i =>
                {
                    var date = start.AddDays(i);
                    return orders
                        .Where(o => o.NgayTao.Date == date)
                        .Sum(o => o.TongThanhToan);
                })
                .ToList();
        }
        else if (period == "monthly")
        {
            var months = new List<string>();
            var data = new List<decimal>();

            for (var date = new DateTime(start.Year, start.Month, 1);
                 date <= end;
                 date = date.AddMonths(1))
            {
                months.Add(date.ToString("MM/yyyy"));
                var monthRevenue = orders
                    .Where(o => o.NgayTao.Year == date.Year
                        && o.NgayTao.Month == date.Month)
                    .Sum(o => o.TongThanhToan);
                data.Add(monthRevenue);
            }

            report.ChartLabels = months;
            report.ChartData = data;
        }
        else // weekly
        {
            var weeks = new List<string>();
            var data = new List<decimal>();

            var currentStart = start;
            while (currentStart <= end)
            {
                var weekEnd = currentStart.AddDays(6);
                if (weekEnd > end) weekEnd = end;

                weeks.Add($"{currentStart:dd/MM} - {weekEnd:dd/MM}");

                var weekRevenue = orders
                    .Where(o => o.NgayTao >= currentStart
                        && o.NgayTao <= weekEnd.AddDays(1).AddSeconds(-1))
                    .Sum(o => o.TongThanhToan);
                data.Add(weekRevenue);

                currentStart = currentStart.AddDays(7);
            }

            report.ChartLabels = weeks;
            report.ChartData = data;
        }

        // Top sản phẩm bán chạy
        report.TopProducts = await _context.ChiTietDonHangs
            .Where(cd => cd.IddonHangNavigation!.IdtrangThai != OrderStatusIds.Cancelled
                        && cd.IddonHangNavigation.NgayTao >= start &&
                        cd.IddonHangNavigation.NgayTao <= end.AddDays(1).AddSeconds(-1))
            .GroupBy(cd => new
            {
                cd.IdbienTheNavigation!.IdsanPhamNavigation!.IdsanPham,
                cd.IdbienTheNavigation.IdsanPhamNavigation.TenSanPham
            })
            .Select(g => new TopProductViewModel
            {
                ProductId = g.Key.IdsanPham,
                ProductName = g.Key.TenSanPham ?? string.Empty,
                QuantitySold = g.Sum(cd => cd.SoLuong),
                Revenue = g.Sum(cd => cd.SoLuong * cd.DonGia)
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(10)
            .ToListAsync();

        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> CustomerReport(DateTime? startDate, DateTime? endDate)
    {
        var end = endDate ?? DateTime.Now.Date;
        var start = startDate ?? end.AddMonths(-3);

        var report = new CustomerReportViewModel
        {
            StartDate = start,
            EndDate = end
        };

        // Tổng số khách hàng
        report.TotalCustomers = await _context.NguoiDungs
            .Where(u => u.NgayTao <= end)
            .CountAsync();

        // Khách hàng mới trong kỳ
        report.NewCustomers = await _context.NguoiDungs
            .Where(u => u.NgayTao >= start && u.NgayTao <= end.AddDays(1).AddSeconds(-1))
            .CountAsync();

        // Khách hàng có đơn hàng
        report.ActiveCustomers = await _context.DonHangs
            .Where(o => o.IdtrangThai != OrderStatusIds.Cancelled
                && o.NgayTao >= start
                && o.NgayTao <= end.AddDays(1).AddSeconds(-1))
            .Select(o => o.IdnguoiDung)
            .Distinct()
            .CountAsync();

        // Top khách hàng
        report.TopCustomers = await _context.DonHangs
            .Where(o => o.IdtrangThai != OrderStatusIds.Cancelled
                && o.NgayTao >= start
                && o.NgayTao <= end.AddDays(1).AddSeconds(-1))
            .GroupBy(o => new
            {
                o.IdnguoiDung,
                o.IdnguoiDungNavigation!.HoTen,
                o.IdnguoiDungNavigation.Email
            })
            .Select(g => new TopCustomerViewModel
            {
                CustomerId = g.Key.IdnguoiDung ?? 0,
                CustomerName = g.Key.HoTen ?? string.Empty,
                Email = g.Key.Email ?? string.Empty,
                TotalOrders = g.Count(),
                TotalSpent = g.Sum(o => o.TongThanhToan)
            })
            .OrderByDescending(c => c.TotalSpent)
            .Take(20)
            .ToListAsync();

        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> ProductPerformance(DateTime? startDate, DateTime? endDate, int? categoryId)
    {
        var end = endDate ?? DateTime.Now.Date;
        var start = startDate ?? end.AddMonths(-1);

        var report = new ProductPerformanceViewModel
        {
            StartDate = start,
            EndDate = end,
            CategoryId = categoryId
        };

        // Danh sách danh mục  
        report.Categories = await _context.DanhMucs
            .Select(c => new { c.IddanhMuc, c.TenDanhMuc })
            .ToDictionaryAsync(c => c.IddanhMuc, c => c.TenDanhMuc ?? string.Empty);

        // Query sản phẩm
        var query = _context.ChiTietDonHangs
            .Where(cd => cd.IddonHangNavigation!.IdtrangThai != OrderStatusIds.Cancelled
                        && cd.IddonHangNavigation.NgayTao >= start &&
                        cd.IddonHangNavigation.NgayTao <= end.AddDays(1).AddSeconds(-1));

        if (categoryId.HasValue)
        {
            query = query.Where(cd => cd.IdbienTheNavigation!.IdsanPhamNavigation!.IddanhMuc == categoryId.Value);
        }

        report.Products = await query
            .GroupBy(cd => new
            {
                cd.IdbienTheNavigation!.IdsanPhamNavigation!.IdsanPham,
                cd.IdbienTheNavigation.IdsanPhamNavigation.TenSanPham,
                cd.IdbienTheNavigation.IdsanPhamNavigation.IddanhMuc,
                CategoryName = cd.IdbienTheNavigation.IdsanPhamNavigation.IddanhMucNavigation!.TenDanhMuc
            })
            .Select(g => new ProductPerformanceItemViewModel
            {
                ProductId = g.Key.IdsanPham,
                ProductName = g.Key.TenSanPham ?? string.Empty,
                CategoryName = g.Key.CategoryName ?? string.Empty,
                QuantitySold = g.Sum(cd => cd.SoLuong),
                Revenue = g.Sum(cd => cd.SoLuong * cd.DonGia),
                OrderCount = g.Select(cd => cd.IddonHang).Distinct().Count()
            })
            .OrderByDescending(p => p.Revenue)
            .ToListAsync();

        return View(report);
    }
}
