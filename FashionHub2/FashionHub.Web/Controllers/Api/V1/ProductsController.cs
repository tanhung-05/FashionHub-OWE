using FashionHub.Web.Application.Common;
using FashionHub.Web.Application.Products;
using FashionHub.Web.Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.Controllers.Api.V1;

[ApiController]
[Route("api/v1/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService productService;

    public ProductsController(IProductService productService)
    {
        this.productService = productService;
    }

    /// <summary>Returns the active product catalog with filtering and pagination.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProductSummaryDto>>> GetProducts(
        [FromQuery] ProductQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await productService.GetProductsAsync(query, cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>Returns an active product with sellable variants and images.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailDto>> GetProduct(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await productService.GetProductAsync(id, cancellationToken);
        return this.ToActionResult(result);
    }
}
