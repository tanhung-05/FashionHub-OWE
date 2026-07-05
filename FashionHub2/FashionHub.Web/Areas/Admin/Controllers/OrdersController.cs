using System.Globalization;
using System.Text;
using FashionHub.Web.Data;
using FashionHub.Web.Models.Generated;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController : Controller
{
    private const int PendingStatusId = 0;
    private const int ConfirmedStatusId = 1;
    private const int CancelledStatusId = 4;

    private readonly ApplicationDbContext _context;

    public OrdersController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? searchString, int? statusId, string? fromDate, string? toDate)
    {
        var orders = _context.DonHangs
            .Include(order => order.IdnguoiDungNavigation)
            .Include(order => order.IdtrangThaiNavigation)
            .Include(order => order.IdphuongThucThanhToanNavigation)
            .OrderByDescending(order => order.NgayTao)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var keyword = searchString.Trim();

            if (int.TryParse(keyword, out var orderId))
            {
                orders = orders.Where(order => order.IddonHang == orderId);
            }
            else
            {
                orders = orders.Where(order => order.TenNguoiNhan.Contains(keyword));
            }
        }

        if (statusId.HasValue)
        {
            orders = orders.Where(order => order.IdtrangThai == statusId.Value);
        }

        if (DateTime.TryParse(fromDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedFromDate))
        {
            orders = orders.Where(order => order.NgayTao >= parsedFromDate);
        }

        if (DateTime.TryParse(toDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedToDate))
        {
            var endDate = parsedToDate.Date.AddDays(1);
            orders = orders.Where(order => order.NgayTao < endDate);
        }

        ViewBag.StatusList = await _context.TrangThaiDonHangs
            .OrderBy(status => status.IdtrangThai)
            .ToListAsync();

        ViewBag.SearchString = searchString;
        ViewBag.StatusId = statusId;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;

        return View(await orders.ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return BadRequest();
        }

        var order = await _context.DonHangs
            .Include(order => order.ChiTietDonHangs)
                .ThenInclude(detail => detail.IdbienTheNavigation)
                    .ThenInclude(variant => variant!.IdsanPhamNavigation)
            .Include(order => order.ChiTietDonHangs)
                .ThenInclude(detail => detail.IdbienTheNavigation)
                    .ThenInclude(variant => variant!.IdmauSacNavigation)
            .Include(order => order.ChiTietDonHangs)
                .ThenInclude(detail => detail.IdbienTheNavigation)
                    .ThenInclude(variant => variant!.IdkichThuocNavigation)
            .Include(order => order.IdtrangThaiNavigation)
            .Include(order => order.IdphuongThucThanhToanNavigation)
            .Include(order => order.IdmaGiamGiaNavigation)
            .FirstOrDefaultAsync(order => order.IddonHang == id.Value);

        if (order is null)
        {
            return NotFound();
        }

        ViewBag.StatusList = new SelectList(
            await _context.TrangThaiDonHangs.OrderBy(status => status.IdtrangThai).ToListAsync(),
            nameof(TrangThaiDonHang.IdtrangThai),
            nameof(TrangThaiDonHang.TenTrangThai),
            order.IdtrangThai);

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, int idTrangThai)
    {
        var order = await _context.DonHangs
            .Include(order => order.ChiTietDonHangs)
            .FirstOrDefaultAsync(order => order.IddonHang == id);

        if (order is null)
        {
            return NotFound();
        }

        var oldStatus = order.IdtrangThai;

        if (idTrangThai == CancelledStatusId && oldStatus != CancelledStatusId)
        {
            foreach (var item in order.ChiTietDonHangs)
            {
                if (!item.IdbienThe.HasValue)
                {
                    continue;
                }

                var variant = await _context.BienTheSanPhams.FindAsync(item.IdbienThe.Value);
                if (variant is not null)
                {
                    variant.SoLuongTon += item.SoLuong;
                }
            }
        }
        else if (oldStatus == CancelledStatusId && idTrangThai != CancelledStatusId)
        {
            foreach (var item in order.ChiTietDonHangs)
            {
                if (!item.IdbienThe.HasValue)
                {
                    continue;
                }

                var variant = await _context.BienTheSanPhams.FindAsync(item.IdbienThe.Value);
                if (variant is null)
                {
                    continue;
                }

                if (variant.SoLuongTon < item.SoLuong)
                {
                    TempData["Error"] = $"Không thể khôi phục đơn. Sản phẩm {item.TenSanPham} không đủ tồn kho!";
                    return RedirectToAction(nameof(Details), new { id });
                }

                variant.SoLuongTon -= item.SoLuong;
            }
        }

        order.IdtrangThai = idTrangThai;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật trạng thái thành công!";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Invoice(int id)
    {
        var order = await _context.DonHangs
            .Include(order => order.ChiTietDonHangs)
                .ThenInclude(detail => detail.IdbienTheNavigation)
                    .ThenInclude(variant => variant!.IdsanPhamNavigation)
            .Include(order => order.ChiTietDonHangs)
                .ThenInclude(detail => detail.IdbienTheNavigation)
                    .ThenInclude(variant => variant!.IdmauSacNavigation)
            .Include(order => order.ChiTietDonHangs)
                .ThenInclude(detail => detail.IdbienTheNavigation)
                    .ThenInclude(variant => variant!.IdkichThuocNavigation)
            .FirstOrDefaultAsync(order => order.IddonHang == id);

        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    public async Task<IActionResult> ExportExcel()
    {
        var data = await _context.DonHangs
            .Include(order => order.IdnguoiDungNavigation)
            .Include(order => order.IdtrangThaiNavigation)
            .OrderByDescending(order => order.NgayTao)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Mã Đơn,Khách Hàng,SĐT,Ngày Đặt,Tổng Tiền,Trạng Thái,Địa Chỉ Giao");

        foreach (var item in data)
        {
            var customer = EscapeCsv(item.TenNguoiNhan);
            var phone = EscapeCsv(item.SoDienThoai);
            var createdAt = item.NgayTao?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
            var status = EscapeCsv(item.IdtrangThaiNavigation.TenTrangThai);
            var address = EscapeCsv(item.DiaChiGiao);

            csv.AppendLine($"{item.IddonHang},{customer},{phone},{createdAt},{item.TongThanhToan},{status},{address}");
        }

        var content = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(csv.ToString()))
            .ToArray();

        return File(content, "text/csv; charset=utf-8", $"DanhSachDonHang_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkPrint(int[] selectedIds)
    {
        if (selectedIds.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn ít nhất một đơn hàng để in.";
            return RedirectToAction(nameof(Index));
        }

        var orders = await _context.DonHangs
            .Include(order => order.ChiTietDonHangs)
                .ThenInclude(detail => detail.IdbienTheNavigation)
                    .ThenInclude(variant => variant!.IdsanPhamNavigation)
            .Include(order => order.ChiTietDonHangs)
                .ThenInclude(detail => detail.IdbienTheNavigation)
                    .ThenInclude(variant => variant!.IdmauSacNavigation)
            .Include(order => order.ChiTietDonHangs)
                .ThenInclude(detail => detail.IdbienTheNavigation)
                    .ThenInclude(variant => variant!.IdkichThuocNavigation)
            .Where(order => selectedIds.Contains(order.IddonHang))
            .OrderBy(order => order.IddonHang)
            .ToListAsync();

        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<JsonResult> ConfirmOrder(int id)
    {
        var order = await _context.DonHangs.FindAsync(id);

        if (order is null)
        {
            return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
        }

        if (order.IdtrangThai != PendingStatusId)
        {
            return Json(new { success = false, message = "Đơn hàng không ở trạng thái chờ." });
        }

        order.IdtrangThai = ConfirmedStatusId;
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = $"Đã xác nhận đơn hàng #{id}" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<JsonResult> ConfirmAllPending()
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var pendingOrders = await _context.DonHangs
                .Where(order => order.IdtrangThai == PendingStatusId)
                .ToListAsync();

            if (pendingOrders.Count == 0)
            {
                return Json(new { success = false, message = "Không có đơn nào cần xác nhận." });
            }

            foreach (var order in pendingOrders)
            {
                order.IdtrangThai = ConfirmedStatusId;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Json(new { success = true, message = $"Đã xác nhận thành công {pendingOrders.Count} đơn hàng!" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
        }
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"");
        return escaped.Contains(',') || escaped.Contains('"') || escaped.Contains('\n') || escaped.Contains('\r')
            ? $"\"{escaped}\""
            : escaped;
    }
}