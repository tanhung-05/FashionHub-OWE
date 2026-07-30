using FashionHub.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.ViewComponents;

public class CartIconViewComponent : ViewComponent
{
    private readonly ICartService cartService;

    public CartIconViewComponent(ICartService cartService)
    {
        this.cartService = cartService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var result = await cartService.GetCartAsync();
        ViewBag.CartItemCount = result.Value?.TotalQuantity ?? 0;
        return View();
    }
}
