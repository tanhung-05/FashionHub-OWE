using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.Controllers;

[Route("blog")]
public sealed class BlogController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }
}
