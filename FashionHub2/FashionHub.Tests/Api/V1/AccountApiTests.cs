using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FashionHub.Web.Application.Accounts;
using FashionHub.Web.Application.Authentication;
using FluentAssertions;

namespace FashionHub.Tests.Api.V1;

public class AccountApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> factory;

    public AccountApiTests(CustomWebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Profile_AnonymousUser_ReturnsUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/account/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType
            .Should().Be("application/problem+json");
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("title").GetString()
            .Should().Be("Authentication required");
        problem.RootElement.GetProperty("status").GetInt32()
            .Should().Be((int)HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Home_ContentSecurityPolicy_AllowsVietnamAddressApi()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        response.Headers.GetValues("Content-Security-Policy")
            .Should().ContainSingle()
            .Which.Should().Contain("https://provinces.open-api.vn");
    }

    [Fact]
    public async Task Profile_CustomerCanReadAndUpdateWithoutExposingSecurityStamp()
    {
        using var client = CreateClient();
        await RegisterCustomerAsync(client);

        var updateResponse = await client.PutAsJsonAsync(
            "/api/v1/account/profile",
            new UpdateAccountProfileRequest
            {
                FullName = "Updated Customer",
                Email = $"updated.{Guid.NewGuid():N}@example.com",
                PhoneNumber = null
            });

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<AccountProfileDto>();
        updated!.FullName.Should().Be("Updated Customer");

        var meResponse = await client.GetAsync("/api/v1/auth/me");
        meResponse.EnsureSuccessStatusCode();
        var json = await meResponse.Content.ReadAsStringAsync();
        json.ToLowerInvariant().Should().NotContain("securitystamp");
    }

    [Fact]
    public async Task Profile_DuplicateEmail_ReturnsConflictProblem()
    {
        using var client = CreateClient();
        await RegisterCustomerAsync(client);

        var response = await client.PutAsJsonAsync(
            "/api/v1/account/profile",
            new UpdateAccountProfileRequest
            {
                FullName = "Duplicate Email",
                Email = "admin@example.com"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("code").GetString()
            .Should().Be("email-already-exists");
    }

    [Fact]
    public async Task Profile_UpdateWithoutAntiforgeryToken_ReturnsBadRequest()
    {
        using var client = CreateClient();
        await RegisterCustomerAsync(client);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");

        var response = await client.PutAsJsonAsync(
            "/api/v1/account/profile",
            new UpdateAccountProfileRequest
            {
                FullName = "No Token",
                Email = $"no-token.{Guid.NewGuid():N}@example.com"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Addresses_CustomerCanCreateSetDefaultAndDeleteOwnedAddresses()
    {
        using var client = CreateClient();
        await RegisterCustomerAsync(client);

        var first = await CreateAddressAsync(client, "First Street");
        first.IsDefault.Should().BeTrue();
        var second = await CreateAddressAsync(client, "Second Street");
        second.IsDefault.Should().BeFalse();

        var setDefaultResponse = await client.PutAsync(
            $"/api/v1/account/addresses/{second.Id}/default",
            content: null);
        setDefaultResponse.EnsureSuccessStatusCode();

        var deleteResponse = await client.DeleteAsync(
            $"/api/v1/account/addresses/{second.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var remaining = await client.GetFromJsonAsync<List<AddressDto>>(
            "/api/v1/account/addresses");
        remaining.Should().ContainSingle();
        remaining![0].Id.Should().Be(first.Id);
        remaining[0].IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Address_FromAnotherCustomer_ReturnsNotFound()
    {
        using var ownerClient = CreateClient();
        await RegisterCustomerAsync(ownerClient);
        var address = await CreateAddressAsync(ownerClient, "Owned Street");

        using var otherClient = CreateClient();
        await RegisterCustomerAsync(otherClient);
        var response = await otherClient.GetAsync(
            $"/api/v1/account/addresses/{address.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Password_CustomerCanChangePasswordAndCurrentSessionIsRevoked()
    {
        using var client = CreateClient();
        var email = await RegisterCustomerAsync(client);

        var response = await client.PutAsJsonAsync(
            "/api/v1/account/password",
            new ChangeAccountPasswordRequest
            {
                CurrentPassword = "Secure123!",
                NewPassword = "NewSecure456!",
                ConfirmPassword = "NewSecure456!"
            });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync("/api/v1/account/profile")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

        await client.RefreshCsrfTokenAsync();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest
            {
                Email = email,
                Password = "NewSecure456!"
            });
        loginResponse.EnsureSuccessStatusCode();
    }

    private async Task<string> RegisterCustomerAsync(HttpClient client)
    {
        var email = $"account.{Guid.NewGuid():N}@example.com";
        await client.RefreshCsrfTokenAsync();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest
            {
                FullName = "Account API Customer",
                Email = email,
                Password = "Secure123!",
                ConfirmPassword = "Secure123!"
            });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await client.RefreshCsrfTokenAsync();
        return email;
    }

    private static async Task<AddressDto> CreateAddressAsync(
        HttpClient client,
        string street)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/account/addresses",
            new SaveAddressRequest
            {
                RecipientName = "API Customer",
                PhoneNumber = "0912345678",
                Street = street,
                Ward = "Test Ward",
                District = "Test District",
                Province = "Test Province"
            });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<AddressDto>())!;
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false
            });
}
