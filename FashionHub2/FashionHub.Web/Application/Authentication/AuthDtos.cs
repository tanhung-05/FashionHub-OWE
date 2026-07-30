using System.ComponentModel.DataAnnotations;
using FashionHub.Web.Application.Common;

namespace FashionHub.Web.Application.Authentication;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public sealed class RegisterRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed record AuthUserDto(
    int Id,
    string FullName,
    string Email,
    string Role);

public interface IAuthService
{
    Task<ServiceResult<AuthUserDto>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AuthUserDto>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AuthUserDto>> GetCurrentUserAsync(
        CancellationToken cancellationToken = default);
}

public interface IAuthenticationSessionService
{
    Task SignInAsync(AuthUserDto user, bool rememberMe);

    Task SignOutAsync();
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
