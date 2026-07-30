using System.Globalization;
using System.Text;
using FashionHub.Web.Application.Admin;
using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using FashionHub.Web.Models.Generated;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
[AutoValidateAntiforgeryToken]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAdminOrderService _orderService;

    public OrdersController(
        ApplicationDbContext context,
        IAdminOrderService orderService)
    {
        _context = context;
        _orderService = orderService;
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

        var allowedStatusIds = OrderStatusTransitions
            .GetAllowedNextStatusIds(order.IdtrangThai)
            .Append(order.IdtrangThai)
            .ToArray();

        var allowedStatuses = await _context.TrangThaiDonHangs
            .Where(status => allowedStatusIds.Contains(status.IdtrangThai))
            .OrderBy(status => status.IdtrangThai)
            .ToListAsync();

        ViewBag.StatusList = new SelectList(
            allowedStatuses,
            nameof(TrangThaiDonHang.IdtrangThai),
            nameof(TrangThaiDonHang.TenTrangThai),
            order.IdtrangThai);

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        int id,
        int idTrangThai,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.UpdateStatusAsync(
            id,
            new UpdateOrderStatusRequest
            {
                StatusId = idTrangThai,
                Note = "Admin cập nhật trạng thái từ giao diện quản trị"
            },
            cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error!.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

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
            var createdAt = item.NgayTao.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
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
    public async Task<JsonResult> ConfirmOrder(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.UpdateStatusAsync(
            id,
            new UpdateOrderStatusRequest
            {
                StatusId = OrderStatusIds.Confirmed,
                Note = "Admin xác nhận đơn hàng"
            },
            cancellationToken);

        return result.IsSuccess
            ? Json(new { success = true, message = $"Đã xác nhận đơn hàng #{id}" })
            : Json(new { success = false, message = result.Error!.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<JsonResult> ConfirmAllPending(
        CancellationToken cancellationToken)
    {
        var result = await _orderService.ConfirmAllPendingAsync(cancellationToken);
        return result.IsSuccess
            ? Json(new
            {
                success = true,
                message = $"Đã xác nhận thành công {result.Value} đơn hàng!"
            })
            : Json(new { success = false, message = result.Error!.Message });
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
