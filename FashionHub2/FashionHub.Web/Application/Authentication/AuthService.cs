using FashionHub.Web.Application.Common;
using FashionHub.Web.Data;
using FashionHub.Web.Models.Generated;
using Microsoft.EntityFrameworkCore;

namespace FashionHub.Web.Application.Authentication;

public sealed class AuthService : IAuthService
{
    private const int DefaultCustomerRoleId = 2;
    private readonly ApplicationDbContext dbContext;
    private readonly ICurrentUserService currentUser;
    private readonly IPasswordHasher passwordHasher;
    private readonly ILogger<AuthService> logger;

    public AuthService(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IPasswordHasher passwordHasher,
        ILogger<AuthService> logger)
    {
        this.dbContext = dbContext;
        this.currentUser = currentUser;
        this.passwordHasher = passwordHasher;
        this.logger = logger;
    }

    public async Task<ServiceResult<AuthUserDto>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.NguoiDungs
            .AsNoTracking()
            .Include(item => item.IdvaiTroNavigation)
            .FirstOrDefaultAsync(item =>
                item.Email.ToLower() == normalizedEmail,
                cancellationToken);

        if (user == null
            || !user.TrangThai
            || user.DeletedAt != null
            || !passwordHasher.Verify(request.Password, user.MatKhauHash))
        {
            logger.LogWarning("Authentication attempt failed");
            return InvalidCredentials();
        }

        return ServiceResult<AuthUserDto>.Success(MapUser(user));
    }

    public async Task<ServiceResult<AuthUserDto>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailExists = await dbContext.NguoiDungs
            .AnyAsync(item => item.Email.ToLower() == normalizedEmail, cancellationToken);
        if (emailExists)
        {
            return ServiceResult<AuthUserDto>.Failure(
                ServiceErrorType.Conflict,
                "email-already-exists",
                "Email này đã được sử dụng.");
        }

        var customerRole = await dbContext.VaiTros
            .FirstOrDefaultAsync(role => role.IdvaiTro == DefaultCustomerRoleId, cancellationToken);
        if (customerRole == null)
        {
            throw new InvalidOperationException("The default customer role is not configured.");
        }

        var user = new NguoiDung
        {
            HoTen = request.FullName.Trim(),
            Email = normalizedEmail,
            MatKhauHash = passwordHasher.Hash(request.Password),
            IdvaiTro = customerRole.IdvaiTro,
            IdvaiTroNavigation = customerRole,
            NgayTao = DateTime.Now,
            TrangThai = true
        };

        dbContext.NguoiDungs.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<AuthUserDto>.Success(MapUser(user));
    }

    public async Task<ServiceResult<AuthUserDto>> GetCurrentUserAsync(
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.UserId.HasValue)
        {
            return ServiceResult<AuthUserDto>.Failure(
                ServiceErrorType.Unauthorized,
                "authentication-required",
                "Vui lòng đăng nhập.");
        }

        var user = await dbContext.NguoiDungs
            .AsNoTracking()
            .Include(item => item.IdvaiTroNavigation)
            .FirstOrDefaultAsync(item =>
                item.IdnguoiDung == currentUser.UserId.Value
                && item.TrangThai
                && item.DeletedAt == null,
                cancellationToken);
        return user == null
            ? ServiceResult<AuthUserDto>.Failure(
                ServiceErrorType.Unauthorized,
                "authentication-required",
                "Vui lòng đăng nhập.")
            : ServiceResult<AuthUserDto>.Success(MapUser(user));
    }

    private static AuthUserDto MapUser(NguoiDung user) =>
        new(
            user.IdnguoiDung,
            user.HoTen,
            user.Email,
            user.IdvaiTroNavigation.TenVaiTro);

    private static ServiceResult<AuthUserDto> InvalidCredentials() =>
        ServiceResult<AuthUserDto>.Failure(
            ServiceErrorType.Unauthorized,
            "invalid-credentials",
            "Email hoặc mật khẩu không đúng.");
}
