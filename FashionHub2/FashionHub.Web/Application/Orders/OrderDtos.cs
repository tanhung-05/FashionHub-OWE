using System.ComponentModel.DataAnnotations;
using FashionHub.Web.Application.Common;

namespace FashionHub.Web.Application.Orders;

public sealed class OrderQueryParameters
{
    private const int MaximumPageSize = 50;
    private int pageSize = 10;

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, MaximumPageSize)]
    public int PageSize
    {
        get => pageSize;
        set => pageSize = Math.Clamp(value, 1, MaximumPageSize);
    }

    [Range(0, int.MaxValue)]
    public int? StatusId { get; set; }
}

public sealed class CreateOrderRequest
{
    [Range(1, int.MaxValue)]
    public int AddressId { get; set; }

    [Range(1, int.MaxValue)]
    public int PaymentMethodId { get; set; }

    [StringLength(50)]
    public string? CouponCode { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}

public sealed record OrderSummaryDto(
    int Id,
    DateTime CreatedAt,
    decimal Total,
    int StatusId,
    string Status,
    int TotalQuantity);

public sealed record OrderItemDto(
    int Id,
    int? VariantId,
    string ProductName,
    string? ColorName,
    string? SizeName,
    decimal UnitPrice,
    int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed record ShippingAddressDto(
    string RecipientName,
    string PhoneNumber,
    string FullAddress);

public sealed record OrderDetailDto(
    int Id,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    ShippingAddressDto ShippingAddress,
    decimal Subtotal,
    decimal ShippingFee,
    decimal Discount,
    decimal Total,
    int StatusId,
    string Status,
    string? PaymentMethod,
    string? PaymentMethodCode,
    byte PaymentStatusId,
    DateTime? PaidAt,
    string? Note,
    IReadOnlyList<OrderItemDto> Items);

public interface IOrderService
{
    Task<ServiceResult<PagedResult<OrderSummaryDto>>> GetOrdersAsync(
        OrderQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<OrderDetailDto>> GetOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<OrderDetailDto>> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default);
}
