using System.Security.Claims;
using FashionHub.Web.Application.Authentication;
using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using FashionHub.Web.Models.Generated;
using FashionHub.Web.Services;
using FashionHub.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext dbContext;
    private readonly ICartService cartService;
    private readonly IAuthService authService;
    private readonly IAuthenticationSessionService authenticationSession;

    public AccountController(
        ApplicationDbContext dbContext,
        ICartService cartService,
        IAuthService authService,
        IAuthenticationSessionService authenticationSession)
    {
        this.dbContext = dbContext;
        this.cartService = cartService;
        this.authService = authService;
        this.authenticationSession = authenticationSession;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await authService.LoginAsync(new LoginRequest
        {
            Email = model.Email,
            Password = model.Password,
            RememberMe = model.RememberMe
        });
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!.Message);
            return View(model);
        }

        await authenticationSession.SignInAsync(result.Value!, model.RememberMe);
        await cartService.MergeGuestCartAsync(result.Value!.Id);

        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await authService.RegisterAsync(new RegisterRequest
        {
            FullName = model.FullName,
            Email = model.Email,
            Password = model.Password,
            ConfirmPassword = model.ConfirmPassword
        });
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(nameof(model.Email), result.Error!.Message);
            return View(model);
        }

        await authenticationSession.SignInAsync(result.Value!, rememberMe: false);
        await cartService.MergeGuestCartAsync(result.Value!.Id);

        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await authenticationSession.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    #region Profile Management

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction(nameof(Login));

        var user = await dbContext.NguoiDungs.FindAsync(userId.Value);
        if (user == null)
            return NotFound();

        var model = new ProfileViewModel
        {
            HoTen = user.HoTen ?? string.Empty,
            Email = user.Email ?? string.Empty,
            SoDienThoai = user.SoDienThoai
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction(nameof(Login));

        var user = await dbContext.NguoiDungs.FindAsync(userId.Value);
        if (user == null)
            return NotFound();

        // Check if email is already taken by another user
        var emailExists = await dbContext.NguoiDungs
            .AnyAsync(u => u.Email == model.Email && u.IdnguoiDung != userId.Value);

        if (emailExists)
        {
            ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác");
            return View(model);
        }

        user.HoTen = model.HoTen;
        user.Email = model.Email;
        user.SoDienThoai = model.SoDienThoai;

        await dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";

        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction(nameof(Login));

        var user = await dbContext.NguoiDungs.FindAsync(userId.Value);
        if (user == null)
            return NotFound();

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.MatKhauHash))
        {
            ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng");
            return View(model);
        }

        // Update to new password
        user.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
        return RedirectToAction(nameof(Profile));
    }

    #endregion

    #region Address Management

    [Authorize]
    public async Task<IActionResult> Addresses()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction(nameof(Login));

        var addresses = await dbContext.DiaChis
            .Where(d => d.IdnguoiDung == userId.Value)
            .OrderByDescending(d => d.LaMacDinh)
            .ThenByDescending(d => d.IddiaChi)
            .ToListAsync();

        return View(addresses);
    }

    [Authorize]
    public IActionResult CreateAddress()
    {
        return View(new AddressManagementViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAddress(AddressManagementViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction(nameof(Login));

        // If this is set as default, unset all other default addresses
        if (model.LaMacDinh)
        {
            var existingAddresses = await dbContext.DiaChis
                .Where(d => d.IdnguoiDung == userId.Value && d.LaMacDinh == true)
                .ToListAsync();

            foreach (var addr in existingAddresses)
            {
                addr.LaMacDinh = false;
            }
        }

        var newAddress = new DiaChi
        {
            IdnguoiDung = userId.Value,
            TenNguoiNhan = model.TenNguoiNhan,
            SoDienThoai = model.SoDienThoai,
            ChiTiet = model.ChiTiet,
            PhuongXa = model.PhuongXa,
            QuanHuyen = model.QuanHuyen,
            TinhThanh = model.TinhThanh,
            LaMacDinh = model.LaMacDinh
        };

        dbContext.DiaChis.Add(newAddress);
        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Thêm địa chỉ thành công!";
        return RedirectToAction(nameof(Addresses));
    }

    [Authorize]
    public async Task<IActionResult> EditAddress(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction(nameof(Login));

        var address = await dbContext.DiaChis
            .FirstOrDefaultAsync(d => d.IddiaChi == id && d.IdnguoiDung == userId.Value);

        if (address == null)
            return NotFound();

        var model = new AddressManagementViewModel
        {
            IdDiaChi = address.IddiaChi,
            TenNguoiNhan = address.TenNguoiNhan ?? string.Empty,
            SoDienThoai = address.SoDienThoai ?? string.Empty,
            ChiTiet = address.ChiTiet ?? string.Empty,
            PhuongXa = address.PhuongXa ?? string.Empty,
            QuanHuyen = address.QuanHuyen ?? string.Empty,
            TinhThanh = address.TinhThanh ?? string.Empty,
            LaMacDinh = address.LaMacDinh
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAddress(int id, AddressManagementViewModel model)
    {
        if (id != model.IdDiaChi)
            return NotFound();

        if (!ModelState.IsValid)
            return View(model);

        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction(nameof(Login));

        var address = await dbContext.DiaChis
            .FirstOrDefaultAsync(d => d.IddiaChi == id && d.IdnguoiDung == userId.Value);

        if (address == null)
            return NotFound();

        // If this is set as default, unset all other default addresses
        if (model.LaMacDinh && address.LaMacDinh != true)
        {
            var existingAddresses = await dbContext.DiaChis
                .Where(d => d.IdnguoiDung == userId.Value && d.LaMacDinh == true)
                .ToListAsync();

            foreach (var addr in existingAddresses)
            {
                addr.LaMacDinh = false;
            }
        }

        address.TenNguoiNhan = model.TenNguoiNhan;
        address.SoDienThoai = model.SoDienThoai;
        address.ChiTiet = model.ChiTiet;
        address.PhuongXa = model.PhuongXa;
        address.QuanHuyen = model.QuanHuyen;
        address.TinhThanh = model.TinhThanh;
        address.LaMacDinh = model.LaMacDinh;

        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cập nhật địa chỉ thành công!";
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction(nameof(Login));

        var address = await dbContext.DiaChis
            .FirstOrDefaultAsync(d => d.IddiaChi == id && d.IdnguoiDung == userId.Value);

        if (address == null)
            return NotFound();

        dbContext.DiaChis.Remove(address);
        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Xóa địa chỉ thành công!";
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SetDefaultAddress(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Json(new { success = false, message = "Vui lòng đăng nhập" });

        var address = await dbContext.DiaChis
            .FirstOrDefaultAsync(d => d.IddiaChi == id && d.IdnguoiDung == userId.Value);

        if (address == null)
            return Json(new { success = false, message = "Không tìm thấy địa chỉ" });

        // Unset all other default addresses
        var existingAddresses = await dbContext.DiaChis
            .Where(d => d.IdnguoiDung == userId.Value && d.LaMacDinh == true)
            .ToListAsync();

        foreach (var addr in existingAddresses)
        {
            addr.LaMacDinh = false;
        }

        // Set this one as default
        address.LaMacDinh = true;
        await dbContext.SaveChangesAsync();

        return Json(new { success = true, message = "Đặt địa chỉ mặc định thành công!" });
    }

    #endregion

    #region Order History

    [Authorize]
    public async Task<IActionResult> OrderHistory(int page = 1, int? statusFilter = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction(nameof(Login));

        var pageSize = 10;

        var query = dbContext.DonHangs
            .Where(d => d.IdnguoiDung == userId.Value)
            .Include(d => d.IdtrangThaiNavigation)
            .OrderByDescending(d => d.NgayTao)
            .AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(d => d.IdtrangThai == statusFilter.Value);

        var totalOrders = await query.CountAsync();
        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new OrderHistoryViewModel
            {
                IddonHang = d.IddonHang,
                NgayTao = d.NgayTao,
                TongThanhToan = d.TongThanhToan,
                TrangThai = d.IdtrangThaiNavigation.TenTrangThai,
                MauTrangThai = GetStatusBadgeColor(d.IdtrangThai),
                SoLuongSanPham = d.ChiTietDonHangs.Sum(ct => ct.SoLuong)
            })
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);
        ViewBag.StatusFilter = statusFilter;
        ViewBag.TotalOrders = totalOrders;

        // Get all statuses for filter dropdown
        ViewBag.Statuses = await dbContext.TrangThaiDonHangs.ToListAsync();

        return View(orders);
    }

    [Authorize]
    public async Task<IActionResult> OrderDetail(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction(nameof(Login));

        var order = await dbContext.DonHangs
            .Include(d => d.ChiTietDonHangs)
                .ThenInclude(ct => ct.IdbienTheNavigation)
                    .ThenInclude(bt => bt!.IdsanPhamNavigation)
            .Include(d => d.ChiTietDonHangs)
                .ThenInclude(ct => ct.IdbienTheNavigation)
                    .ThenInclude(bt => bt!.IdmauSacNavigation)
            .Include(d => d.ChiTietDonHangs)
                .ThenInclude(ct => ct.IdbienTheNavigation)
                    .ThenInclude(bt => bt!.IdkichThuocNavigation)
            .Include(d => d.IdtrangThaiNavigation)
            .Include(d => d.IdphuongThucThanhToanNavigation)
            .FirstOrDefaultAsync(d => d.IddonHang == id && d.IdnguoiDung == userId.Value);

        if (order == null)
            return NotFound();

        var model = new OrderDetailViewModel
        {
            IddonHang = order.IddonHang,
            NgayTao = order.NgayTao,
            TenNguoiNhan = order.TenNguoiNhan,
            DiaChiGiao = order.DiaChiGiao,
            SoDienThoai = order.SoDienThoai,
            TongTienHang = order.TongTienHang,
            PhiVanChuyen = order.PhiVanChuyen,
            TienGiamGia = order.TienGiamGia,
            TongThanhToan = order.TongThanhToan,
            TrangThai = order.IdtrangThaiNavigation.TenTrangThai,
            IdtrangThai = order.IdtrangThai,
            PhuongThucThanhToan = order.IdphuongThucThanhToanNavigation?.TenPhuongThuc,
            Items = order.ChiTietDonHangs.Select(ct => new OrderItemViewModel
            {
                TenSanPham = ct.IdbienTheNavigation?.IdsanPhamNavigation?.TenSanPham ?? "N/A",
                HinhAnh = ct.IdbienTheNavigation?.HinhAnhBienThes.FirstOrDefault()?.IdhinhAnhNavigation?.DuongDan,
                MauSac = ct.IdbienTheNavigation?.IdmauSacNavigation?.TenMau,
                KichThuoc = ct.IdbienTheNavigation?.IdkichThuocNavigation?.TenKichThuoc,
                SoLuong = ct.SoLuong,
                DonGia = ct.DonGia,
                ThanhTien = ct.SoLuong * ct.DonGia
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CancelOrder(int id, string reason)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Json(new { success = false, message = "Vui lòng đăng nhập" });

        var order = await dbContext.DonHangs
            .Include(d => d.ChiTietDonHangs)
            .FirstOrDefaultAsync(d => d.IddonHang == id && d.IdnguoiDung == userId.Value);

        if (order == null)
            return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

        if (order.IdtrangThai != OrderStatusIds.Pending)
            return Json(new { success = false, message = "Không thể hủy đơn hàng ở trạng thái hiện tại" });

        foreach (var item in order.ChiTietDonHangs)
        {
            if (!item.IdbienThe.HasValue)
            {
                continue;
            }

            var variant = await dbContext.BienTheSanPhams.FindAsync(item.IdbienThe.Value);
            if (variant == null)
            {
                continue;
            }

            var previousStock = variant.SoLuongTon;
            variant.SoLuongTon += item.SoLuong;
            variant.TongDaBan = Math.Max(0, variant.TongDaBan - item.SoLuong);
            variant.NgayCapNhat = DateTime.Now;

            dbContext.LichSuTonKhos.Add(new LichSuTonKho
            {
                IdbienThe = variant.IdbienThe,
                IdnguoiThucHien = userId,
                IddonHang = order.IddonHang,
                LoaiThayDoi = InventoryChangeTypes.OrderCancelled,
                SoLuongThayDoi = item.SoLuong,
                TonTruoc = previousStock,
                TonSau = variant.SoLuongTon,
                GhiChu = $"Khách hàng hủy đơn #{order.IddonHang}",
                NgayTao = DateTime.Now
            });
        }

        order.IdtrangThai = OrderStatusIds.Cancelled;
        order.GhiChu = string.IsNullOrWhiteSpace(reason) ? order.GhiChu : reason.Trim();
        order.NgayCapNhat = DateTime.Now;
        dbContext.LichSuDonHangs.Add(new LichSuDonHang
        {
            IddonHang = order.IddonHang,
            IdtrangThaiCu = OrderStatusIds.Pending,
            IdtrangThaiMoi = OrderStatusIds.Cancelled,
            IdnguoiThucHien = userId,
            GhiChu = string.IsNullOrWhiteSpace(reason) ? "Khách hàng hủy đơn" : reason.Trim(),
            NgayTao = DateTime.Now
        });
        await dbContext.SaveChangesAsync();

        return Json(new { success = true, message = "Hủy đơn hàng thành công" });
    }

    private string GetStatusBadgeColor(int statusId)
    {
        return statusId switch
        {
            OrderStatusIds.Pending => "warning",
            OrderStatusIds.Confirmed => "info",
            OrderStatusIds.Shipping => "primary",
            OrderStatusIds.Completed => "success",
            OrderStatusIds.Cancelled => "danger",
            _ => "secondary"
        };
    }

    #endregion

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
