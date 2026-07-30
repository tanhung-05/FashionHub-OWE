using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.Controllers;

[Route("lien-he")]
public sealed class ContactController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }
}
