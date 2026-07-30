using FashionHub.Web.Application.Authentication;
using FashionHub.Web.Application.Email;
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
    private readonly IPasswordResetService passwordResetService;
    private readonly IPasswordResetLinkFactory passwordResetLinkFactory;
    private readonly IEmailSender emailSender;
    private readonly ILogger<AuthController> logger;

    public AuthController(
        IAuthService authService,
        IAuthenticationSessionService authenticationSession,
        ICartService cartService,
        IPasswordResetService passwordResetService,
        IPasswordResetLinkFactory passwordResetLinkFactory,
        IEmailSender emailSender,
        ILogger<AuthController> logger)
    {
        this.authService = authService;
        this.authenticationSession = authenticationSession;
        this.cartService = cartService;
        this.passwordResetService = passwordResetService;
        this.passwordResetLinkFactory = passwordResetLinkFactory;
        this.emailSender = emailSender;
        this.logger = logger;
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

    /// <summary>Sends a one-time password reset link when the account exists.</summary>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<PasswordResetAcceptedResponse>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<PasswordResetAcceptedResponse>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var ticket = await passwordResetService.CreateTokenAsync(
            request.Email,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        if (ticket != null)
        {
            await TrySendPasswordResetEmailAsync(ticket, cancellationToken);
        }

        return Accepted(new PasswordResetAcceptedResponse(
            "Nếu email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi."));
    }

    /// <summary>Sets a new password using a valid one-time reset token.</summary>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await passwordResetService.ResetPasswordAsync(
            request.Token,
            request.NewPassword,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        await authenticationSession.SignOutAsync();
        return NoContent();
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

    private async Task TrySendPasswordResetEmailAsync(
        PasswordResetTicket ticket,
        CancellationToken cancellationToken)
    {
        try
        {
            var resetUrl = passwordResetLinkFactory.Create(ticket.Token);

            await emailSender.SendPasswordResetAsync(
                ticket.Email,
                ticket.FullName,
                resetUrl,
                ticket.ExpiresAtUtc,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Could not send a password reset email.");
        }
    }
}
