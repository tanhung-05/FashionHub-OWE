using System.Security.Cryptography;
using System.Text;
using FashionHub.Web.Application.Common;
using FashionHub.Web.Data;
using FashionHub.Web.Models.Generated;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FashionHub.Web.Application.Authentication;

public sealed class PasswordResetService : IPasswordResetService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IPasswordHasher passwordHasher;
    private readonly PasswordResetOptions options;
    private readonly TimeProvider timeProvider;

    public PasswordResetService(
        ApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IOptions<PasswordResetOptions> options,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.passwordHasher = passwordHasher;
        this.options = options.Value;
        this.timeProvider = timeProvider;
    }

    public async Task<PasswordResetTicket?> CreateTokenAsync(
        string email,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await dbContext.NguoiDungs
            .FirstOrDefaultAsync(
                item =>
                    item.Email.ToLower() == normalizedEmail
                    && item.TrangThai
                    && item.DeletedAt == null,
                cancellationToken);

        if (user == null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var existingTokens = await dbContext.DatLaiMatKhauTokens
            .Where(item =>
                item.IdnguoiDung == user.IdnguoiDung
                && item.NgaySuDungUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var existingToken in existingTokens)
        {
            existingToken.NgaySuDungUtc = now;
        }

        var rawToken = WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(32));
        var expiresAt = now.AddMinutes(
            Math.Clamp(options.TokenLifetimeMinutes, 5, 120));

        dbContext.DatLaiMatKhauTokens.Add(new DatLaiMatKhauToken
        {
            IdnguoiDung = user.IdnguoiDung,
            TokenHash = HashToken(rawToken),
            NgayHetHanUtc = expiresAt,
            NgayTaoUtc = now,
            DiaChiIp = string.IsNullOrWhiteSpace(ipAddress)
                ? null
                : ipAddress[..Math.Min(ipAddress.Length, 45)]
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new PasswordResetTicket(
            user.Email,
            user.HoTen,
            rawToken,
            expiresAt);
    }

    public async Task<bool> IsTokenValidAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var tokenHash = HashToken(token);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return await dbContext.DatLaiMatKhauTokens
            .AsNoTracking()
            .AnyAsync(item =>
                item.TokenHash == tokenHash
                && item.NgaySuDungUtc == null
                && item.NgayHetHanUtc > now
                && item.IdnguoiDungNavigation.TrangThai
                && item.IdnguoiDungNavigation.DeletedAt == null,
                cancellationToken);
    }

    public async Task<ServiceResult<bool>> ResetPasswordAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return InvalidToken();
        }

        var tokenHash = HashToken(token);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var resetToken = await dbContext.DatLaiMatKhauTokens
            .Include(item => item.IdnguoiDungNavigation)
            .FirstOrDefaultAsync(
                item => item.TokenHash == tokenHash,
                cancellationToken);

        if (resetToken == null
            || resetToken.NgaySuDungUtc != null
            || resetToken.NgayHetHanUtc <= now
            || !resetToken.IdnguoiDungNavigation.TrangThai
            || resetToken.IdnguoiDungNavigation.DeletedAt != null)
        {
            return InvalidToken();
        }

        var activeTokens = await dbContext.DatLaiMatKhauTokens
            .Where(item =>
                item.IdnguoiDung == resetToken.IdnguoiDung
                && item.NgaySuDungUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var activeToken in activeTokens)
        {
            activeToken.NgaySuDungUtc = now;
        }

        resetToken.IdnguoiDungNavigation.MatKhauHash =
            passwordHasher.Hash(newPassword);
        resetToken.IdnguoiDungNavigation.SecurityStamp = Guid.NewGuid();
        resetToken.IdnguoiDungNavigation.NgayCapNhat =
            timeProvider.GetLocalNow().DateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static ServiceResult<bool> InvalidToken() =>
        ServiceResult<bool>.Failure(
            ServiceErrorType.Validation,
            "invalid-password-reset-token",
            "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
}
