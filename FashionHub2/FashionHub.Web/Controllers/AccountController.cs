using System.Security.Claims;
using FashionHub.Web.Application.Authentication;
using FashionHub.Web.Application.Email;
using FashionHub.Web.Data;
using FashionHub.Web.Domain;
using FashionHub.Web.Models.Generated;
using FashionHub.Web.Services;
using FashionHub.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext dbContext;
    private readonly ICartService cartService;
    private readonly IAuthService authService;
    private readonly IAuthenticationSessionService authenticationSession;
    private readonly IPasswordResetService passwordResetService;
    private readonly IPasswordResetLinkFactory passwordResetLinkFactory;
    private readonly IEmailSender emailSender;
    private readonly ILogger<AccountController> logger;

    public AccountController(
        ApplicationDbContext dbContext,
        ICartService cartService,
        IAuthService authService,
        IAuthenticationSessionService authenticationSession,
        IPasswordResetService passwordResetService,
        IPasswordResetLinkFactory passwordResetLinkFactory,
        IEmailSender emailSender,
        ILogger<AccountController> logger)
    {
        this.dbContext = dbContext;
        this.cartService = cartService;
        this.authService = authService;
        this.authenticationSession = authenticationSession;
        this.passwordResetService = passwordResetService;
        this.passwordResetLinkFactory = passwordResetLinkFactory;
        this.emailSender = emailSender;
        this.logger = logger;
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

        if (string.Equals(result.Value.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
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

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ticket = await passwordResetService.CreateTokenAsync(
            model.Email,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        if (ticket != null)
        {
            await TrySendPasswordResetEmailAsync(ticket, cancellationToken);
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ResetPassword(
        string? token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)
            || !await passwordResetService.IsTokenValidAsync(
                token,
                cancellationToken))
        {
            return View("ResetPasswordInvalid");
        }

        return View(new ResetPasswordViewModel { Token = token });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await passwordResetService.ResetPasswordAsync(
            model.Token,
            model.NewPassword,
            cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!.Message);
            return View(model);
        }

        await authenticationSession.SignOutAsync();
        return RedirectToAction(nameof(ResetPasswordConfirmation));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResetPasswordConfirmation()
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

        var user = await dbContext.NguoiDungs
            .AsNoTracking()
            .Include(item => item.IdvaiTroNavigation)
            .FirstOrDefaultAsync(item => item.IdnguoiDung == userId.Value);
        if (user == null)
            return NotFound();

        var model = new ProfileViewModel
        {
            HoTen = user.HoTen ?? string.Empty,
            Email = user.Email ?? string.Empty,
            SoDienThoai = user.SoDienThoai,
            NgayThamGia = user.NgayTao,
            TenVaiTro = user.IdvaiTroNavigation?.TenVaiTro ?? "Khách hàng"
        };

        await PopulateProfileOverviewAsync(model, userId.Value);
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
        {
            await PopulateProfileOverviewAsync(model, userId.Value);
            return View(model);
        }

        var user = await dbContext.NguoiDungs.FindAsync(userId.Value);
        if (user == null)
            return NotFound();

        // Check if email is already taken by another user
        var emailExists = await dbContext.NguoiDungs
            .AnyAsync(u => u.Email == model.Email && u.IdnguoiDung != userId.Value);

        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng bởi tài khoản khác");
            await PopulateProfileOverviewAsync(model, userId.Value);
            return View(model);
        }

        user.HoTen = model.HoTen;
        user.Email = model.Email;
        user.SoDienThoai = model.SoDienThoai;
        user.NgayCapNhat = DateTime.Now;

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
        user.SecurityStamp = Guid.NewGuid();
        await dbContext.SaveChangesAsync();

        await authenticationSession.SignOutAsync();
        TempData["SuccessMessage"] =
            "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
        return RedirectToAction(nameof(Login));
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

        var hasAddress = await dbContext.DiaChis
            .AnyAsync(address => address.IdnguoiDung == userId.Value);
        var shouldBeDefault = model.LaMacDinh || !hasAddress;

        if (shouldBeDefault)
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
            LaMacDinh = shouldBeDefault,
            NgayTao = DateTime.Now
        };

        dbContext.DiaChis.Add(newAddress);
        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Thêm địa chỉ thành công!";
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAddressAjax(AddressManagementViewModel model)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new
            {
                success = false,
                message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."
            });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Distinct()
                .ToList();

            return BadRequest(new
            {
                success = false,
                message = errors.FirstOrDefault() ?? "Thông tin địa chỉ chưa hợp lệ.",
                errors
            });
        }

        var hasAddress = await dbContext.DiaChis
            .AnyAsync(address => address.IdnguoiDung == userId.Value);
        var address = new DiaChi
        {
            IdnguoiDung = userId.Value,
            TenNguoiNhan = model.TenNguoiNhan.Trim(),
            SoDienThoai = model.SoDienThoai.Trim(),
            ChiTiet = model.ChiTiet.Trim(),
            PhuongXa = model.PhuongXa.Trim(),
            QuanHuyen = model.QuanHuyen.Trim(),
            TinhThanh = model.TinhThanh.Trim(),
            LaMacDinh = !hasAddress || model.LaMacDinh,
            NgayTao = DateTime.Now
        };

        if (address.LaMacDinh)
        {
            var defaultAddresses = await dbContext.DiaChis
                .Where(item =>
                    item.IdnguoiDung == userId.Value
                    && item.LaMacDinh)
                .ToListAsync();
            foreach (var defaultAddress in defaultAddresses)
            {
                defaultAddress.LaMacDinh = false;
            }
        }

        dbContext.DiaChis.Add(address);
        await dbContext.SaveChangesAsync();

        return Json(new
        {
            success = true,
            message = "Đã lưu địa chỉ giao hàng.",
            newAddress = new
            {
                iddiaChi = address.IddiaChi,
                tenNguoiNhan = address.TenNguoiNhan,
                soDienThoai = address.SoDienThoai,
                laMacDinh = address.LaMacDinh,
                fullAddress = FormatAddress(address)
            }
        });
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

        var wasDefault = address.LaMacDinh;
        dbContext.DiaChis.Remove(address);
        if (wasDefault)
        {
            var replacement = await dbContext.DiaChis
                .Where(item =>
                    item.IdnguoiDung == userId.Value
                    && item.IddiaChi != id)
                .OrderByDescending(item => item.IddiaChi)
                .FirstOrDefaultAsync();
            if (replacement != null)
            {
                replacement.LaMacDinh = true;
            }
        }

        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Xóa địa chỉ thành công!";
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
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
            .Include(d => d.ChiTietDonHangs)
                .ThenInclude(ct => ct.IdbienTheNavigation)
                    .ThenInclude(bt => bt!.HinhAnhBienThes)
                        .ThenInclude(image => image.IdhinhAnhNavigation)
            .Include(d => d.IdtrangThaiNavigation)
            .Include(d => d.IdphuongThucThanhToanNavigation)
            .FirstOrDefaultAsync(d => d.IddonHang == id && d.IdnguoiDung == userId.Value);

        if (order == null)
            return NotFound();

        var productIds = order.ChiTietDonHangs
            .Where(item => item.IdbienTheNavigation != null)
            .Select(item => item.IdbienTheNavigation!.IdsanPham)
            .Distinct()
            .ToArray();
        var existingReviews = await dbContext.DanhGia
            .AsNoTracking()
            .Where(review =>
                review.IdnguoiDung == userId.Value
                && productIds.Contains(review.IdsanPham))
            .ToDictionaryAsync(review => review.IdsanPham);

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
            Items = order.ChiTietDonHangs.Select(ct =>
            {
                var productId = ct.IdbienTheNavigation?.IdsanPham;
                existingReviews.TryGetValue(productId ?? 0, out var review);

                return new OrderItemViewModel
                {
                    IdchiTietDonHang = ct.IdchiTietDonHang,
                    IdsanPham = productId,
                    TenSanPham = ct.TenSanPham,
                    HinhAnh = ct.IdbienTheNavigation?.HinhAnhBienThes
                        .OrderByDescending(image => image.LaAnhChinh)
                        .ThenBy(image => image.ThuTuHienThi)
                        .Select(image => image.IdhinhAnhNavigation.DuongDan)
                        .FirstOrDefault(),
                    MauSac = ct.TenMau,
                    KichThuoc = ct.TenKichThuoc,
                    SoLuong = ct.SoLuong,
                    DonGia = ct.DonGia,
                    ThanhTien = ct.SoLuong * ct.DonGia,
                    CoTheDanhGia = order.IdtrangThai == OrderStatusIds.Completed
                        && productId.HasValue
                        && review == null,
                    DaDanhGia = review != null,
                    DiemDanhGia = review?.DiemSo,
                    NoiDungDanhGia = review?.NoiDung,
                    NgayDanhGia = review?.NgayTao
                };
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReview(
        CreateReviewViewModel model,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
                ?? "Thông tin đánh giá không hợp lệ.";
            return RedirectToAction(nameof(OrderDetail), new { id = model.IddonHang });
        }

        var orderItem = await dbContext.ChiTietDonHangs
            .Include(item => item.IddonHangNavigation)
            .Include(item => item.IdbienTheNavigation)
            .FirstOrDefaultAsync(
                item =>
                    item.IdchiTietDonHang == model.IdchiTietDonHang
                    && item.IddonHang == model.IddonHang
                    && item.IddonHangNavigation.IdnguoiDung == userId.Value,
                cancellationToken);

        if (orderItem == null)
            return NotFound();

        if (orderItem.IddonHangNavigation.IdtrangThai != OrderStatusIds.Completed)
        {
            TempData["ErrorMessage"] = "Chỉ có thể đánh giá sản phẩm trong đơn hàng đã hoàn thành.";
            return RedirectToAction(nameof(OrderDetail), new { id = model.IddonHang });
        }

        var productId = orderItem.IdbienTheNavigation?.IdsanPham;
        if (!productId.HasValue)
        {
            TempData["ErrorMessage"] = "Sản phẩm này hiện không thể đánh giá.";
            return RedirectToAction(nameof(OrderDetail), new { id = model.IddonHang });
        }

        var hasExistingReview = await dbContext.DanhGia.AnyAsync(
            review =>
                review.IdnguoiDung == userId.Value
                && review.IdsanPham == productId.Value,
            cancellationToken);
        if (hasExistingReview)
        {
            TempData["ErrorMessage"] = "Bạn đã đánh giá sản phẩm này.";
            return RedirectToAction(nameof(OrderDetail), new { id = model.IddonHang });
        }

        dbContext.DanhGia.Add(new DanhGia
        {
            IdnguoiDung = userId.Value,
            IdsanPham = productId.Value,
            IdchiTietDonHang = orderItem.IdchiTietDonHang,
            DiemSo = (byte)model.DiemSo,
            NoiDung = string.IsNullOrWhiteSpace(model.NoiDung)
                ? null
                : model.NoiDung.Trim(),
            TrangThai = true,
            NgayTao = DateTime.Now
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(
                exception,
                "Unable to create review for user {UserId} and product {ProductId}",
                userId.Value,
                productId.Value);
            TempData["ErrorMessage"] = "Không thể lưu đánh giá. Vui lòng thử lại.";
            return RedirectToAction(nameof(OrderDetail), new { id = model.IddonHang });
        }

        TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá sản phẩm.";
        return RedirectToAction(nameof(OrderDetail), new { id = model.IddonHang });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
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

        TempData["SuccessMessage"] = "Hủy đơn hàng thành công.";
        return Json(new { success = true, message = "Hủy đơn hàng thành công" });
    }

    private static string GetStatusBadgeColor(int statusId)
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

    private async Task PopulateProfileOverviewAsync(
        ProfileViewModel model,
        int userId)
    {
        var overview = await dbContext.NguoiDungs
            .AsNoTracking()
            .Where(user => user.IdnguoiDung == userId)
            .Select(user => new
            {
                user.NgayTao,
                RoleName = user.IdvaiTroNavigation.TenVaiTro,
                AddressCount = user.DiaChis.Count,
                OrderCount = user.DonHangs.Count,
                ProcessingOrderCount = user.DonHangs.Count(order =>
                    order.IdtrangThai == OrderStatusIds.Pending
                    || order.IdtrangThai == OrderStatusIds.Confirmed
                    || order.IdtrangThai == OrderStatusIds.Shipping)
            })
            .FirstOrDefaultAsync();

        if (overview != null)
        {
            model.NgayThamGia = overview.NgayTao;
            model.TenVaiTro = overview.RoleName;
            model.SoDiaChi = overview.AddressCount;
            model.SoDonHang = overview.OrderCount;
            model.SoDonDangXuLy = overview.ProcessingOrderCount;
        }

        model.DiaChiMacDinh = await dbContext.DiaChis
            .AsNoTracking()
            .Where(address => address.IdnguoiDung == userId)
            .OrderByDescending(address => address.LaMacDinh)
            .ThenByDescending(address => address.IddiaChi)
            .Select(address => new ProfileAddressSummaryViewModel
            {
                TenNguoiNhan = address.TenNguoiNhan,
                SoDienThoai = address.SoDienThoai,
                DiaChiDayDu = FormatAddress(address)
            })
            .FirstOrDefaultAsync();
    }

    private static string FormatAddress(DiaChi address) =>
        $"{address.ChiTiet}, {address.PhuongXa}, {address.QuanHuyen}, {address.TinhThanh}";

    private async Task TrySendPasswordResetEmailAsync(
        PasswordResetTicket ticket,
        CancellationToken cancellationToken)
    {
        try
        {
            var resetUrl = passwordResetLinkFactory.Create(ticket.Token);

            await emailSender.SendPasswordResetAsync(
                ticket.Email,
                ticket.FullName,
                resetUrl,
                ticket.ExpiresAtUtc,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Could not send a password reset email.");
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
