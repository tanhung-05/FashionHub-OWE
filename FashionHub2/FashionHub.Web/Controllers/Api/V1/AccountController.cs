using FashionHub.Web.Application.Accounts;
using FashionHub.Web.Application.Authentication;
using FashionHub.Web.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FashionHub.Web.Controllers.Api.V1;

[ApiController]
[Authorize]
[Route("api/v1/account")]
public sealed class AccountController : ControllerBase
{
    private readonly IAccountService accountService;
    private readonly IAuthService authService;
    private readonly IAuthenticationSessionService authenticationSession;

    public AccountController(
        IAccountService accountService,
        IAuthService authService,
        IAuthenticationSessionService authenticationSession)
    {
        this.accountService = accountService;
        this.authService = authService;
        this.authenticationSession = authenticationSession;
    }

    /// <summary>Returns the current customer's profile.</summary>
    [HttpGet("profile")]
    [ProducesResponseType<AccountProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AccountProfileDto>> GetProfile(
        CancellationToken cancellationToken)
    {
        var result = await accountService.GetProfileAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>Updates the current customer's profile.</summary>
    [HttpPut("profile")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<AccountProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccountProfileDto>> UpdateProfile(
        [FromBody] UpdateAccountProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accountService.UpdateProfileAsync(
            request,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        var currentAuthUser = await authService.GetCurrentUserAsync(cancellationToken);
        if (currentAuthUser.IsSuccess)
        {
            await authenticationSession.SignInAsync(
                currentAuthUser.Value!,
                rememberMe: false);
        }

        return Ok(result.Value);
    }

    /// <summary>Changes the password and revokes the current session.</summary>
    [HttpPut("password")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangeAccountPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accountService.ChangePasswordAsync(
            request,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result).Result!;
        }

        await authenticationSession.SignOutAsync();
        return NoContent();
    }

    /// <summary>Returns all addresses owned by the current customer.</summary>
    [HttpGet("addresses")]
    [ProducesResponseType<IReadOnlyList<AddressDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AddressDto>>> GetAddresses(
        CancellationToken cancellationToken)
    {
        var result = await accountService.GetAddressesAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>Returns one address owned by the current customer.</summary>
    [HttpGet("addresses/{id:int}")]
    [ProducesResponseType<AddressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddressDto>> GetAddress(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await accountService.GetAddressAsync(id, cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>Creates an address for the current customer.</summary>
    [HttpPost("addresses")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<AddressDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AddressDto>> CreateAddress(
        [FromBody] SaveAddressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accountService.CreateAddressAsync(
            request,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetAddress),
            new { id = result.Value!.Id },
            result.Value);
    }

    /// <summary>Updates an address owned by the current customer.</summary>
    [HttpPut("addresses/{id:int}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<AddressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddressDto>> UpdateAddress(
        int id,
        [FromBody] SaveAddressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accountService.UpdateAddressAsync(
            id,
            request,
            cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>Deletes an address owned by the current customer.</summary>
    [HttpDelete("addresses/{id:int}")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddress(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await accountService.DeleteAddressAsync(id, cancellationToken);
        return result.IsSuccess
            ? NoContent()
            : this.ToActionResult(result).Result!;
    }

    /// <summary>Sets an owned address as the default delivery address.</summary>
    [HttpPut("addresses/{id:int}/default")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<AddressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddressDto>> SetDefaultAddress(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await accountService.SetDefaultAddressAsync(
            id,
            cancellationToken);
        return this.ToActionResult(result);
    }
}
