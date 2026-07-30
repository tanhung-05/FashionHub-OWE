using FashionHub.Web.Application.Cart;
using FashionHub.Web.Infrastructure.Web;
using FashionHub.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.Controllers.Api.V1;

[ApiController]
[Route("api/v1/cart")]
public sealed class CartController : ControllerBase
{
    private readonly ICartService cartService;

    public CartController(ICartService cartService)
    {
        this.cartService = cartService;
    }

    /// <summary>Returns the current authenticated or guest cart.</summary>
    [HttpGet]
    [ProducesResponseType<CartDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartDto>> GetCart(CancellationToken cancellationToken)
    {
        var result = await cartService.GetCartAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>Adds a product variant to the current cart.</summary>
    [HttpPost("items")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<CartDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CartDto>> AddItem(
        [FromBody] AddCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await cartService.AddAsync(
            request.VariantId,
            request.Quantity,
            cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>Replaces the quantity of a cart variant.</summary>
    [HttpPut("items/{variantId:int}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<CartDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CartDto>> UpdateItem(
        int variantId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await cartService.UpdateAsync(
            variantId,
            request.Quantity,
            cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>Removes a product variant from the current cart.</summary>
    [HttpDelete("items/{variantId:int}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<CartDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> RemoveItem(
        int variantId,
        CancellationToken cancellationToken)
    {
        var result = await cartService.RemoveAsync(variantId, cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>Removes all items from the current cart.</summary>
    [HttpDelete]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<CartDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CartDto>> Clear(CancellationToken cancellationToken)
    {
        var result = await cartService.ClearAsync(cancellationToken);
        return this.ToActionResult(result);
    }
}
