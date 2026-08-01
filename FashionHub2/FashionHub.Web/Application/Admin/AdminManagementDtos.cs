using System.ComponentModel.DataAnnotations;
using FashionHub.Web.Application.Common;

namespace FashionHub.Web.Application.Admin;

public sealed class AdminManagementQuery
{
    [StringLength(100)]
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public sealed record AdminCategoryDto(
    int Id,
    string Name,
    string? Slug,
    int? ParentId,
    string? ParentName,
    int ProductCount,
    int ChildCount,
    bool IsActive);

public sealed class SaveAdminCategoryRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    public int? ParentId { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed record AdminCouponDto(
    int Id,
    string Code,
    string? Name,
    int DiscountType,
    decimal Value,
    decimal MinimumOrder,
    decimal? MaximumDiscount,
    int Quantity,
    int UsedCount,
    DateTime StartsAt,
    DateTime EndsAt,
    bool IsActive);

public sealed class SaveAdminCouponRequest
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Name { get; set; }

    [Range(1, 2)]
    public int DiscountType { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Value { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal MinimumOrder { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? MaximumDiscount { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed record AdminCustomerDto(
    int Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    DateTime CreatedAt,
    bool IsActive,
    int OrderCount,
    decimal TotalSpent);

public sealed record AdminCustomerOrderDto(
    int Id,
    DateTime CreatedAt,
    decimal Total,
    int StatusId,
    string StatusName);

public sealed record AdminCustomerDetailDto(
    AdminCustomerDto Customer,
    IReadOnlyList<AdminCustomerOrderDto> Orders);

public interface IAdminManagementService
{
    Task<ServiceResult<PagedResult<AdminCategoryDto>>> GetCategoriesAsync(
        AdminManagementQuery query,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AdminCategoryDto>> GetCategoryAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AdminCategoryDto>> CreateCategoryAsync(
        SaveAdminCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AdminCategoryDto>> UpdateCategoryAsync(
        int id,
        SaveAdminCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteCategoryAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<AdminCouponDto>>> GetCouponsAsync(
        AdminManagementQuery query,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AdminCouponDto>> GetCouponAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AdminCouponDto>> CreateCouponAsync(
        SaveAdminCouponRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AdminCouponDto>> UpdateCouponAsync(
        int id,
        SaveAdminCouponRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteCouponAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AdminCouponDto>> ToggleCouponAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<AdminCustomerDto>>> GetCustomersAsync(
        AdminManagementQuery query,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AdminCustomerDetailDto>> GetCustomerAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AdminCustomerDto>> ToggleCustomerAsync(
        int id,
        CancellationToken cancellationToken = default);
}
