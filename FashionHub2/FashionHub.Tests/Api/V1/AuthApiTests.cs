using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FashionHub.Web.Application.Authentication;
using FluentAssertions;

namespace FashionHub.Tests.Api.V1;

public class AuthApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> factory;

    public AuthApiTests(CustomWebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Login_ValidCredentials_CreatesCookieUsedByMe()
    {
        using var client = CreateClient();

        await client.LoginAsCustomerAsync();
        var response = await client.GetAsync("/api/v1/auth/me");

        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<AuthUserDto>();
        user!.Email.Should().Be("test@example.com");
        user.Role.Should().Be("Customer");
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsGenericProblemWithoutPasswordData()
    {
        using var client = CreateClient();
        await client.RefreshCsrfTokenAsync();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = "missing@example.com",
            Password = "WrongPassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotContain("MatKhauHash");
        json.Should().NotContain("WrongPassword!");
        JsonDocument.Parse(json).RootElement
            .GetProperty("code").GetString().Should().Be("invalid-credentials");
    }

    [Fact]
    public async Task Register_CreatesCustomerAndRejectsDuplicateEmail()
    {
        using var client = CreateClient();
        await client.RefreshCsrfTokenAsync();
        var request = new RegisterRequest
        {
            FullName = "New Customer",
            Email = "new.customer@example.com",
            Password = "Secure123!",
            ConfirmPassword = "Secure123!"
        };

        var createdResponse = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        createdResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await createdResponse.Content.ReadFromJsonAsync<AuthUserDto>();
        user!.Role.Should().Be("Customer");

        await client.RefreshCsrfTokenAsync();
        var duplicateResponse = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Logout_RemovesAuthenticationCookie()
    {
        using var client = CreateClient();
        await client.LoginAsCustomerAsync();

        var logoutResponse = await client.PostAsync("/api/v1/auth/logout", content: null);
        var meResponse = await client.GetAsync("/api/v1/auth/me");

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        meResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false
            });
}
