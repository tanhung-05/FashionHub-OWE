using FashionHub.Web.Application.Admin;
using FashionHub.Web.Application.Common;
using FashionHub.Web.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.Controllers.Api.V1.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/products")]
public sealed class AdminProductsController : ControllerBase
{
    private readonly IAdminProductService productService;

    public AdminProductsController(IAdminProductService productService)
    {
        this.productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminProductDto>>> GetProducts(
        [FromQuery] AdminProductQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await productService.GetProductsAsync(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminProductDto>> GetProduct(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await productService.GetProductAsync(id, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<AdminProductDto>> CreateProduct(
        [FromBody] SaveAdminProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productService.CreateProductAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetProduct),
            new { id = result.Value!.Id },
            result.Value);
    }

    [HttpPut("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<AdminProductDto>> UpdateProduct(
        int id,
        [FromBody] SaveAdminProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productService.UpdateProductAsync(id, request, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await productService.DeleteProductAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result).Result!;
        }

        return NoContent();
    }
}
