using FashionHub.Web.Application.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace FashionHub.Web.Infrastructure.Authentication;

public sealed class PasswordResetLinkFactory : IPasswordResetLinkFactory
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IHostEnvironment environment;
    private readonly PasswordResetOptions options;

    public PasswordResetLinkFactory(
        IHttpContextAccessor httpContextAccessor,
        IHostEnvironment environment,
        IOptions<PasswordResetOptions> options)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.environment = environment;
        this.options = options.Value;
    }

    public string Create(string token)
    {
        if (!string.IsNullOrWhiteSpace(options.PublicBaseUrl))
        {
            return CreateFromConfiguredBaseUrl(token);
        }

        if (!environment.IsDevelopment()
            && !environment.IsEnvironment("Test"))
        {
            throw new InvalidOperationException(
                "PasswordReset:PublicBaseUrl must be configured outside Development.");
        }

        var request = httpContextAccessor.HttpContext?.Request
            ?? throw new InvalidOperationException("HTTP context is not available.");
        var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/";
        return new Uri(
            new Uri(baseUrl),
            QueryHelpers.AddQueryString(
                "Account/ResetPassword",
                "token",
                token))
            .ToString();
    }

    private string CreateFromConfiguredBaseUrl(string token)
    {
        if (!Uri.TryCreate(
                options.PublicBaseUrl.TrimEnd('/') + "/",
                UriKind.Absolute,
                out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttps
                && baseUri.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException(
                "PasswordReset:PublicBaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        if (environment.IsProduction()
            && baseUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                "PasswordReset:PublicBaseUrl must use HTTPS in Production.");
        }

        return new Uri(
            baseUri,
            QueryHelpers.AddQueryString(
                "Account/ResetPassword",
                "token",
                token))
            .ToString();
    }
}
