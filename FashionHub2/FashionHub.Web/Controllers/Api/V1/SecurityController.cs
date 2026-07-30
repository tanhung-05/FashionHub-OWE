using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.Controllers.Api.V1;

[ApiController]
[Route("api/v1/security")]
public sealed class SecurityController : ControllerBase
{
    private readonly IAntiforgery antiforgery;

    public SecurityController(IAntiforgery antiforgery)
    {
        this.antiforgery = antiforgery;
    }

    /// <summary>Issues an antiforgery token for cookie-authenticated API mutations.</summary>
    [HttpGet("csrf-token")]
    [ProducesResponseType<CsrfTokenResponse>(StatusCodes.Status200OK)]
    public ActionResult<CsrfTokenResponse> GetCsrfToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new CsrfTokenResponse(
            tokens.RequestToken ?? string.Empty,
            "X-CSRF-TOKEN"));
    }
}

public sealed record CsrfTokenResponse(string Token, string HeaderName);
