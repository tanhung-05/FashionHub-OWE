using System.ComponentModel.DataAnnotations;
using FashionHub.Web.Application.Common;
using FashionHub.Web.Application.Orders;

namespace FashionHub.Web.Application.Admin;

public sealed class AdminProductQueryParameters
{
    private int pageSize = 20;

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize
    {
        get => pageSize;
        set => pageSize = Math.Clamp(value, 1, 50);
    }

    [StringLength(100)]
    public string? Search { get; set; }

    public bool IncludeDeleted { get; set; }
}

public class SaveAdminProductRequest
{
    [Required]
    [StringLength(255, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Slug { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? SalePrice { get; set; }

    public DateTime? SaleStart { get; set; }

    public DateTime? SaleEnd { get; set; }

    [Range(1, int.MaxValue)]
    public int? CategoryId { get; set; }

    [Range(1, int.MaxValue)]
    public int? BrandId { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed record AdminProductDto(
    int Id,
    string Name,
    string? Slug,
    string? Description,
    decimal Price,
    decimal? SalePrice,
    DateTime? SaleStart,
    DateTime? SaleEnd,
    int? CategoryId,
    string? CategoryName,
    int? BrandId,
    string? BrandName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? DeletedAt,
    int VariantCount,
    int StockQuantity);

public sealed class AdminOrderQueryParameters
{
    private int pageSize = 20;

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize
    {
        get => pageSize;
        set => pageSize = Math.Clamp(value, 1, 50);
    }

    [StringLength(100)]
    public string? Search { get; set; }

    [Range(0, int.MaxValue)]
    public int? StatusId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }
}

public sealed class UpdateOrderStatusRequest
{
    [Range(0, int.MaxValue)]
    public int StatusId { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}

public sealed record AdminOrderSummaryDto(
    int Id,
    string? CustomerEmail,
    string RecipientName,
    DateTime CreatedAt,
    decimal Total,
    int StatusId,
    string Status,
    int TotalQuantity);

public sealed record AdminOrderDetailDto(
    OrderDetailDto Order,
    int? UserId,
    string? CustomerEmail);

public sealed class AdminReportQuery
{
    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }
}

public sealed record AdminDashboardReportDto(
    DateTime FromDate,
    DateTime ToDate,
    int OrderCount,
    int PendingOrderCount,
    int CompletedOrderCount,
    decimal Revenue,
    int UnitsSold,
    int LowStockVariantCount);

public interface IAdminProductService
{
    Task<ServiceResult<PagedResult<AdminProductDto>>> GetProductsAsync(
        AdminProductQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AdminProductDto>> GetProductAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AdminProductDto>> CreateProductAsync(
        SaveAdminProductRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AdminProductDto>> UpdateProductAsync(
        int productId,
        SaveAdminProductRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteProductAsync(
        int productId,
        CancellationToken cancellationToken = default);
}

public interface IAdminOrderService
{
    Task<ServiceResult<PagedResult<AdminOrderSummaryDto>>> GetOrdersAsync(
        AdminOrderQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AdminOrderDetailDto>> GetOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<AdminOrderDetailDto>> UpdateStatusAsync(
        int orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default);
}

public interface IAdminReportService
{
    Task<ServiceResult<AdminDashboardReportDto>> GetDashboardAsync(
        AdminReportQuery query,
        CancellationToken cancellationToken = default);
}
