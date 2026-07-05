using FashionHub.Web.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.ViewComponents
{
    public class CartIconViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var cart = HttpContext.Session.GetObjectFromJson<Dictionary<int, int>>("Cart") ?? new Dictionary<int, int>();
            ViewBag.CartItemCount = cart.Values.Sum();
            return View();
        }
    }
}