using FashionHub.Web.Application.Authentication;
using FashionHub.Web.Infrastructure.Web;
using FashionHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FashionHub.Web.Controllers.Api.V1;

[ApiController]
[EnableRateLimiting("auth")]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService authService;
    private readonly IAuthenticationSessionService authenticationSession;
    private readonly ICartService cartService;

    public AuthController(
        IAuthService authService,
        IAuthenticationSessionService authenticationSession,
        ICartService cartService)
    {
        this.authService = authService;
        this.authenticationSession = authenticationSession;
        this.cartService = cartService;
    }

    /// <summary>Creates the FashionHub authentication cookie.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<AuthUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthUserDto>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        await authenticationSession.SignInAsync(result.Value!, request.RememberMe);
        await cartService.MergeGuestCartAsync(result.Value!.Id, cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Creates a customer account and signs it in.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<AuthUserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthUserDto>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        await authenticationSession.SignInAsync(result.Value!, rememberMe: false);
        await cartService.MergeGuestCartAsync(result.Value!.Id, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>Removes the current authentication cookie.</summary>
    [Authorize]
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await authenticationSession.SignOutAsync();
        return NoContent();
    }

    /// <summary>Returns the current authenticated user.</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<AuthUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthUserDto>> Me(CancellationToken cancellationToken)
    {
        var result = await authService.GetCurrentUserAsync(cancellationToken);
        return this.ToActionResult(result);
    }
}
