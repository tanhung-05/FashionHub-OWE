using System.Security.Claims;
using FashionHub.Web.Application.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FashionHub.Web.Infrastructure.Authentication;

public sealed class CookieAuthenticationSessionService : IAuthenticationSessionService
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public CookieAuthenticationSessionService(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public Task SignInAsync(AuthUserDto user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Email, user.Email),
            new("FullName", user.FullName),
            new(ClaimTypes.Role, user.Role),
            new("SecurityStamp", user.SecurityStamp.ToString("D"))
        };
        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe
                ? DateTimeOffset.UtcNow.AddDays(14)
                : null
        };

        return HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);
    }

    public Task SignOutAsync() =>
        HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    private HttpContext HttpContext =>
        httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("HTTP context is not available.");
}
