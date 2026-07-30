using System.ComponentModel.DataAnnotations;

namespace FashionHub.Web.Application.Cart;

public sealed record CartItemDto(
    int VariantId,
    int ProductId,
    string ProductName,
    string? ColorName,
    string? SizeName,
    decimal UnitPrice,
    int Quantity,
    string ImageUrl,
    int AvailableStock)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed record CartDto(IReadOnlyList<CartItemDto> Items)
{
    public int TotalQuantity => Items.Sum(item => item.Quantity);

    public decimal Subtotal => Items.Sum(item => item.LineTotal);
}

public sealed class AddCartItemRequest
{
    [Range(1, int.MaxValue)]
    public int VariantId { get; set; }

    [Range(1, 999)]
    public int Quantity { get; set; } = 1;
}

public sealed class UpdateCartItemRequest
{
    [Range(1, 999)]
    public int Quantity { get; set; }
}
