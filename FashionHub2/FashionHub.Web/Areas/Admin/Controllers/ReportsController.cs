using FashionHub.Web.Areas.Admin.ViewModels;
using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using FashionHub.Web.Models.Generated;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
[AutoValidateAntiforgeryToken]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> SalesReport(
        DateTime? startDate,
        DateTime? endDate,
        string period = "daily")
    {
        var end = (endDate ?? DateTime.Today).Date;
        var start = (startDate ?? end.AddDays(-29)).Date;
        period = period is "daily" or "weekly" or "monthly" ? period : "daily";

        if (!IsValidDateRange(start, end)
            || (period == "daily" && (end - start).TotalDays > 366))
        {
            TempData["Error"] = period == "daily"
                ? "Khoảng thời gian theo ngày không được vượt quá 366 ngày."
                : "Khoảng thời gian báo cáo không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var exclusiveEnd = end.AddDays(1);
        var orders = await _context.DonHangs
            .AsNoTracking()
            .Where(order =>
                order.IdtrangThai != OrderStatusIds.Cancelled
                && order.NgayTao >= start
                && order.NgayTao < exclusiveEnd)
            .ToListAsync();

        var report = new SalesReportViewModel
        {
            StartDate = start,
            EndDate = end,
            Period = period,
            TotalOrders = orders.Count,
            TotalRevenue = orders.Sum(order => order.TongThanhToan),
            TotalDiscount = orders.Sum(order => order.TienGiamGia),
            TotalShipping = orders.Sum(order => order.PhiVanChuyen),
            AverageOrderValue = orders.Count == 0
                ? 0
                : orders.Average(order => order.TongThanhToan)
        };

        PopulateSalesChart(report, orders);

        report.TopProducts = await _context.ChiTietDonHangs
            .AsNoTracking()
            .Where(detail =>
                detail.IddonHangNavigation!.IdtrangThai != OrderStatusIds.Cancelled
                && detail.IddonHangNavigation.NgayTao >= start
                && detail.IddonHangNavigation.NgayTao < exclusiveEnd)
            .GroupBy(detail => new
            {
                detail.IdbienTheNavigation!.IdsanPhamNavigation!.IdsanPham,
                detail.IdbienTheNavigation.IdsanPhamNavigation.TenSanPham
            })
            .Select(group => new TopProductViewModel
            {
                ProductId = group.Key.IdsanPham,
                ProductName = group.Key.TenSanPham,
                QuantitySold = group.Sum(detail => detail.SoLuong),
                Revenue = group.Sum(detail => detail.SoLuong * detail.DonGia)
            })
            .OrderByDescending(product => product.QuantitySold)
            .Take(10)
            .ToListAsync();

        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> CustomerReport(
        DateTime? startDate,
        DateTime? endDate)
    {
        var end = (endDate ?? DateTime.Today).Date;
        var start = (startDate ?? end.AddMonths(-3)).Date;
        if (!IsValidDateRange(start, end))
        {
            TempData["Error"] = "Khoảng thời gian báo cáo không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var exclusiveEnd = end.AddDays(1);
        var report = new CustomerReportViewModel
        {
            StartDate = start,
            EndDate = end,
            TotalCustomers = await _context.NguoiDungs
                .AsNoTracking()
                .CountAsync(user =>
                    user.IdvaiTro == 2
                    && user.DeletedAt == null
                    && user.NgayTao < exclusiveEnd),
            NewCustomers = await _context.NguoiDungs
                .AsNoTracking()
                .CountAsync(user =>
                    user.IdvaiTro == 2
                    && user.DeletedAt == null
                    && user.NgayTao >= start
                    && user.NgayTao < exclusiveEnd),
            ActiveCustomers = await _context.DonHangs
                .AsNoTracking()
                .Where(order =>
                    order.IdtrangThai != OrderStatusIds.Cancelled
                    && order.NgayTao >= start
                    && order.NgayTao < exclusiveEnd
                    && order.IdnguoiDung.HasValue)
                .Select(order => order.IdnguoiDung)
                .Distinct()
                .CountAsync()
        };

        report.TopCustomers = await _context.DonHangs
            .AsNoTracking()
            .Where(order =>
                order.IdtrangThai != OrderStatusIds.Cancelled
                && order.NgayTao >= start
                && order.NgayTao < exclusiveEnd
                && order.IdnguoiDung.HasValue
                && order.IdnguoiDungNavigation != null
                && order.IdnguoiDungNavigation.DeletedAt == null)
            .GroupBy(order => new
            {
                order.IdnguoiDung,
                order.IdnguoiDungNavigation!.HoTen,
                order.IdnguoiDungNavigation.Email
            })
            .Select(group => new TopCustomerViewModel
            {
                CustomerId = group.Key.IdnguoiDung!.Value,
                CustomerName = group.Key.HoTen,
                Email = group.Key.Email,
                TotalOrders = group.Count(),
                TotalSpent = group.Sum(order => order.TongThanhToan)
            })
            .OrderByDescending(customer => customer.TotalSpent)
            .Take(20)
            .ToListAsync();

        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> ProductPerformance(
        DateTime? startDate,
        DateTime? endDate,
        int? categoryId)
    {
        var end = (endDate ?? DateTime.Today).Date;
        var start = (startDate ?? end.AddMonths(-1)).Date;
        if (!IsValidDateRange(start, end))
        {
            TempData["Error"] = "Khoảng thời gian báo cáo không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var exclusiveEnd = end.AddDays(1);
        var report = new ProductPerformanceViewModel
        {
            StartDate = start,
            EndDate = end,
            CategoryId = categoryId,
            Categories = await _context.DanhMucs
                .AsNoTracking()
                .Where(category =>
                    category.DeletedAt == null
                    && category.TrangThai)
                .OrderBy(category => category.TenDanhMuc)
                .ToDictionaryAsync(
                    category => category.IddanhMuc,
                    category => category.TenDanhMuc)
        };

        if (categoryId.HasValue && !report.Categories.ContainsKey(categoryId.Value))
        {
            TempData["Error"] = "Danh mục báo cáo không tồn tại hoặc đã ngừng hoạt động.";
            return RedirectToAction(nameof(ProductPerformance));
        }

        var query = _context.ChiTietDonHangs
            .AsNoTracking()
            .Where(detail =>
                detail.IddonHangNavigation!.IdtrangThai != OrderStatusIds.Cancelled
                && detail.IddonHangNavigation.NgayTao >= start
                && detail.IddonHangNavigation.NgayTao < exclusiveEnd);

        if (categoryId.HasValue)
        {
            query = query.Where(detail =>
                detail.IdbienTheNavigation!.IdsanPhamNavigation!.IddanhMuc
                == categoryId.Value);
        }

        report.Products = await query
            .GroupBy(detail => new
            {
                detail.IdbienTheNavigation!.IdsanPhamNavigation!.IdsanPham,
                detail.IdbienTheNavigation.IdsanPhamNavigation.TenSanPham,
                CategoryName = detail.IdbienTheNavigation
                    .IdsanPhamNavigation
                    .IddanhMucNavigation!.TenDanhMuc
            })
            .Select(group => new ProductPerformanceItemViewModel
            {
                ProductId = group.Key.IdsanPham,
                ProductName = group.Key.TenSanPham,
                CategoryName = group.Key.CategoryName,
                QuantitySold = group.Sum(detail => detail.SoLuong),
                Revenue = group.Sum(detail => detail.SoLuong * detail.DonGia),
                OrderCount = group.Select(detail => detail.IddonHang).Distinct().Count()
            })
            .OrderByDescending(product => product.Revenue)
            .ToListAsync();

        return View(report);
    }

    private static void PopulateSalesChart(
        SalesReportViewModel report,
        IReadOnlyCollection<DonHang> orders)
    {
        var start = report.StartDate;
        var end = report.EndDate;

        if (report.Period == "daily")
        {
            var dates = Enumerable.Range(0, (int)(end - start).TotalDays + 1)
                .Select(offset => start.AddDays(offset))
                .ToList();
            report.ChartLabels = dates
                .Select(date => date.ToString("dd/MM"))
                .ToList();
            report.ChartData = dates
                .Select(date => orders
                    .Where(order => order.NgayTao.Date == date)
                    .Sum(order => order.TongThanhToan))
                .ToList();
            return;
        }

        if (report.Period == "monthly")
        {
            for (var date = new DateTime(start.Year, start.Month, 1);
                 date <= end;
                 date = date.AddMonths(1))
            {
                report.ChartLabels.Add(date.ToString("MM/yyyy"));
                report.ChartData.Add(orders
                    .Where(order =>
                        order.NgayTao.Year == date.Year
                        && order.NgayTao.Month == date.Month)
                    .Sum(order => order.TongThanhToan));
            }
            return;
        }

        for (var weekStart = start; weekStart <= end; weekStart = weekStart.AddDays(7))
        {
            var weekEnd = weekStart.AddDays(6);
            if (weekEnd > end)
            {
                weekEnd = end;
            }

            report.ChartLabels.Add($"{weekStart:dd/MM} - {weekEnd:dd/MM}");
            var exclusiveWeekEnd = weekEnd.AddDays(1);
            report.ChartData.Add(orders
                .Where(order =>
                    order.NgayTao >= weekStart
                    && order.NgayTao < exclusiveWeekEnd)
                .Sum(order => order.TongThanhToan));
        }
    }

    private static bool IsValidDateRange(DateTime start, DateTime end) =>
        start <= end && (end - start).TotalDays <= 3650;
}
