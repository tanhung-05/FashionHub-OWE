using System.Net.Http.Json;
using FashionHub.Web.Application.Authentication;
using FashionHub.Web.Controllers.Api.V1;

namespace FashionHub.Tests.Api.V1;

internal static class ApiClientExtensions
{
    public static async Task RefreshCsrfTokenAsync(this HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/security/csrf-token");
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<CsrfTokenResponse>();
        client.DefaultRequestHeaders.Remove(token!.HeaderName);
        client.DefaultRequestHeaders.Add(token.HeaderName, token.Token);
    }

    public static async Task LoginAsCustomerAsync(this HttpClient client)
    {
        await client.RefreshCsrfTokenAsync();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = "test@example.com",
            Password = "Test123!"
        });
        response.EnsureSuccessStatusCode();
        await client.RefreshCsrfTokenAsync();
    }

    public static async Task LoginAsAdminAsync(this HttpClient client)
    {
        await client.RefreshCsrfTokenAsync();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = "admin@example.com",
            Password = "Test123!"
        });
        response.EnsureSuccessStatusCode();
        await client.RefreshCsrfTokenAsync();
    }
}
