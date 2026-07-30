using System.ComponentModel.DataAnnotations;
using FashionHub.Web.Application.Common;

namespace FashionHub.Web.Application.Products;

public sealed class ProductQueryParameters
{
    private const int MaximumPageSize = 50;
    private int pageSize = 20;

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, MaximumPageSize)]
    public int PageSize
    {
        get => pageSize;
        set => pageSize = Math.Clamp(value, 1, MaximumPageSize);
    }

    [StringLength(100)]
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int? CategoryId { get; set; }

    public List<int> BrandIds { get; set; } = new();

    [Range(0, double.MaxValue)]
    public decimal? MinPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MaxPrice { get; set; }

    public bool? InStock { get; set; }

    [RegularExpression("^(name|price|newest)$")]
    public string SortBy { get; set; } = "newest";

    [RegularExpression("^(asc|desc)$")]
    public string SortDirection { get; set; } = "desc";

    public List<int> ColorIds { get; set; } = new();

    public List<int> SizeIds { get; set; } = new();

    public bool OnSaleOnly { get; set; }
}

public sealed record ProductSummaryDto(
    int Id,
    string Name,
    string? Slug,
    decimal Price,
    decimal? SalePrice,
    decimal EffectivePrice,
    DateTime? SaleStart,
    DateTime? SaleEnd,
    string ThumbnailUrl,
    int? CategoryId,
    string? CategoryName,
    int StockQuantity,
    bool IsAvailable,
    DateTime CreatedAt);

public sealed record ProductImageDto(
    int Id,
    string Url,
    string? AltText);

public sealed record ProductVariantDto(
    int Id,
    string Sku,
    int? ColorId,
    string? ColorName,
    string? ColorHex,
    int? SizeId,
    string? SizeName,
    decimal Price,
    int StockQuantity,
    IReadOnlyList<int> ImageIds);

public sealed record ProductDetailDto(
    int Id,
    string Name,
    string? Slug,
    string? Description,
    decimal Price,
    decimal? SalePrice,
    decimal EffectivePrice,
    DateTime? SaleStart,
    DateTime? SaleEnd,
    int? CategoryId,
    string? CategoryName,
    string? BrandName,
    int StockQuantity,
    bool IsAvailable,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<ProductImageDto> Images,
    IReadOnlyList<ProductVariantDto> Variants,
    IReadOnlyList<ProductReviewDto> Reviews,
    IReadOnlyList<ProductSummaryDto> RelatedProducts);

public sealed record CategoryOptionDto(
    int Id,
    string Name,
    int? ParentId);

public sealed record ColorOptionDto(
    int Id,
    string Name,
    string? Hex);

public sealed record SizeOptionDto(
    int Id,
    string Name);

public sealed record BrandOptionDto(
    int Id,
    string Name);

public sealed record ProductReviewDto(
    int Id,
    string CustomerName,
    byte Rating,
    string? Content,
    DateTime CreatedAt);

public sealed record ProductFilterOptionsDto(
    IReadOnlyList<CategoryOptionDto> Categories,
    IReadOnlyList<BrandOptionDto> Brands,
    IReadOnlyList<ColorOptionDto> Colors,
    IReadOnlyList<SizeOptionDto> Sizes);

public interface IProductService
{
    Task<ServiceResult<PagedResult<ProductSummaryDto>>> GetProductsAsync(
        ProductQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<ProductDetailDto>> GetProductAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<ProductFilterOptionsDto>> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default);
}
