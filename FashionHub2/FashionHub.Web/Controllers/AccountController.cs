using System.Security.Claims;
using FashionHub.Web.Data;
using FashionHub.Web.Models.Generated;
using FashionHub.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Controllers;

public class AccountController : Controller
{
    private const int DefaultCustomerRoleId = 2;

    private readonly ApplicationDbContext dbContext;

    public AccountController(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
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

        var user = await dbContext.NguoiDungs
            .Include(u => u.IdvaiTroNavigation)
            .FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.MatKhauHash))
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        if (user.TrangThai == false)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa.");
            return View(model);
        }

        await SignInUserAsync(user, model.RememberMe);

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

        var emailExists = await dbContext.NguoiDungs.AnyAsync(u => u.Email == model.Email);
        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
            return View(model);
        }

        var newUser = new NguoiDung
        {
            HoTen = model.FullName,
            Email = model.Email,
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            IdvaiTro = DefaultCustomerRoleId,
            NgayTao = DateTime.Now,
            TrangThai = true
        };

        dbContext.NguoiDungs.Add(newUser);
        await dbContext.SaveChangesAsync();

        await dbContext.Entry(newUser)
            .Reference(u => u.IdvaiTroNavigation)
            .LoadAsync();

        await SignInUserAsync(newUser, isPersistent: false);

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
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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
            LaMacDinh = address.LaMacDinh ?? false
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
            .FirstOrDefaultAsync(d => d.IddonHang == id && d.IdnguoiDung == userId.Value);

        if (order == null)
            return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

        // Only allow cancel if order is pending (IdtrangThai = 1)
        if (order.IdtrangThai != 1)
            return Json(new { success = false, message = "Không thể hủy đơn hàng ở trạng thái hiện tại" });

        order.IdtrangThai = 5; // Cancelled
        await dbContext.SaveChangesAsync();

        return Json(new { success = true, message = "Hủy đơn hàng thành công" });
    }

    private string GetStatusBadgeColor(int statusId)
    {
        return statusId switch
        {
            1 => "warning",  // Pending
            2 => "info",     // Confirmed
            3 => "primary",  // Shipping
            4 => "success",  // Delivered
            5 => "danger",   // Cancelled
            _ => "secondary"
        };
    }

    #endregion

    private async Task SignInUserAsync(NguoiDung user, bool isPersistent)
    {
        var roleName = user.IdvaiTroNavigation?.TenVaiTro ?? user.IdvaiTro.ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.IdnguoiDung.ToString()),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Email, user.Email),
            new("FullName", user.HoTen),
            new(ClaimTypes.Role, roleName)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        var authenticationProperties = new AuthenticationProperties
        {
            IsPersistent = isPersistent,
            ExpiresUtc = isPersistent
                ? DateTimeOffset.UtcNow.AddDays(14)
                : null
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authenticationProperties);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}