using System.ComponentModel.DataAnnotations;
using FashionHub.Web.Application.Common;

namespace FashionHub.Web.Application.Accounts;

public sealed record AccountProfileDto(
    int Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string Role,
    DateTime CreatedAt);

public sealed class UpdateAccountProfileRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(15)]
    public string? PhoneNumber { get; set; }
}

public sealed class ChangeAccountPasswordRequest
{
    [Required]
    [StringLength(200)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed record AddressDto(
    int Id,
    string RecipientName,
    string PhoneNumber,
    string Street,
    string Ward,
    string District,
    string Province,
    bool IsDefault,
    string FullAddress,
    DateTime CreatedAt);

public sealed class SaveAddressRequest
{
    [Required]
    [StringLength(100)]
    public string RecipientName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Ward { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string District { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Province { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}

public interface IAccountService
{
    Task<ServiceResult<AccountProfileDto>> GetProfileAsync(
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AccountProfileDto>> UpdateProfileAsync(
        UpdateAccountProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> ChangePasswordAsync(
        ChangeAccountPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<AddressDto>>> GetAddressesAsync(
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AddressDto>> GetAddressAsync(
        int addressId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AddressDto>> CreateAddressAsync(
        SaveAddressRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AddressDto>> UpdateAddressAsync(
        int addressId,
        SaveAddressRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteAddressAsync(
        int addressId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AddressDto>> SetDefaultAddressAsync(
        int addressId,
        CancellationToken cancellationToken = default);
}
