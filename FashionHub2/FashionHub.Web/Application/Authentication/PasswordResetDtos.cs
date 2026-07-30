using FashionHub.Web.Application.Common;
using System.ComponentModel.DataAnnotations;

namespace FashionHub.Web.Application.Authentication;

public sealed record PasswordResetTicket(
    string Email,
    string FullName,
    string Token,
    DateTime ExpiresAtUtc);

public sealed class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed record PasswordResetAcceptedResponse(string Message);

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    public int TokenLifetimeMinutes { get; set; } = 30;

    public string PublicBaseUrl { get; set; } = string.Empty;
}

public interface IPasswordResetLinkFactory
{
    string Create(string token);
}

public interface IPasswordResetService
{
    Task<PasswordResetTicket?> CreateTokenAsync(
        string email,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<bool> IsTokenValidAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> ResetPasswordAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);
}
