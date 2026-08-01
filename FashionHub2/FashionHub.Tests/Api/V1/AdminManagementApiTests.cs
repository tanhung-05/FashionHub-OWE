using System.Net;
using System.Net.Http.Json;
using FashionHub.Web.Application.Admin;
using FashionHub.Web.Application.Authentication;
using FashionHub.Web.Application.Common;
using FluentAssertions;

namespace FashionHub.Tests.Api.V1;

public class AdminManagementApiTests
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> factory;

    public AdminManagementApiTests(CustomWebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task ManagementEndpoints_CustomerRole_ReturnsForbidden()
    {
        using var client = CreateClient();
        await client.LoginAsCustomerAsync();

        var response = await client.GetAsync("/api/v1/admin/categories");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Categories_AdminCanCreateUpdateAndSoftDelete()
    {
        using var client = CreateClient();
        await client.LoginAsAdminAsync();
        var uniqueName = $"API Category {Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/admin/categories",
            new SaveAdminCategoryRequest { Name = uniqueName });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var category = await createResponse.Content.ReadFromJsonAsync<AdminCategoryDto>();

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/admin/categories/{category!.Id}",
            new SaveAdminCategoryRequest
            {
                Name = uniqueName + " Updated",
                IsActive = true
            });
        updateResponse.EnsureSuccessStatusCode();

        var deleteResponse = await client.DeleteAsync(
            $"/api/v1/admin/categories/{category.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/v1/admin/categories/{category.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Categories_WithProducts_CannotBeDeleted()
    {
        using var client = CreateClient();
        await client.LoginAsAdminAsync();

        var response = await client.DeleteAsync("/api/v1/admin/categories/1");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Coupons_AdminCanCreateToggleAndDelete()
    {
        using var client = CreateClient();
        await client.LoginAsAdminAsync();
        var code = $"API{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        var request = new SaveAdminCouponRequest
        {
            Code = code,
            Name = "API coupon",
            DiscountType = 2,
            Value = 10,
            MinimumOrder = 100000,
            MaximumDiscount = 50000,
            Quantity = 20,
            StartsAt = DateTime.Now.AddDays(-1),
            EndsAt = DateTime.Now.AddDays(7),
            IsActive = true
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/admin/coupons",
            request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var coupon = await createResponse.Content.ReadFromJsonAsync<AdminCouponDto>();

        var toggleResponse = await client.PutAsync(
            $"/api/v1/admin/coupons/{coupon!.Id}/status",
            content: null);
        toggleResponse.EnsureSuccessStatusCode();
        (await toggleResponse.Content.ReadFromJsonAsync<AdminCouponDto>())!
            .IsActive.Should().BeFalse();

        var deleteResponse = await client.DeleteAsync(
            $"/api/v1/admin/coupons/{coupon.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Coupon_PercentageAboveOneHundred_ReturnsBadRequest()
    {
        using var client = CreateClient();
        await client.LoginAsAdminAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/coupons",
            new SaveAdminCouponRequest
            {
                Code = $"BAD{Guid.NewGuid():N}"[..20],
                DiscountType = 2,
                Value = 101,
                Quantity = 1,
                StartsAt = DateTime.Now,
                EndsAt = DateTime.Now.AddDays(1)
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Customers_AdminCanLockAccountAndRevokeExistingSession()
    {
        using var customerClient = CreateClient();
        var customerEmail = $"lock.{Guid.NewGuid():N}@example.com";
        await customerClient.RefreshCsrfTokenAsync();
        var registerResponse = await customerClient.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest
            {
                FullName = "Lock Test Customer",
                Email = customerEmail,
                Password = "Secure123!",
                ConfirmPassword = "Secure123!"
            });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        await customerClient.RefreshCsrfTokenAsync();

        using var adminClient = CreateClient();
        await adminClient.LoginAsAdminAsync();
        var customers = await adminClient.GetFromJsonAsync<PagedResult<AdminCustomerDto>>(
            $"/api/v1/admin/customers?search={Uri.EscapeDataString(customerEmail)}");
        var customer = customers!.Items.Single(item => item.Email == customerEmail);

        var toggleResponse = await adminClient.PutAsync(
            $"/api/v1/admin/customers/{customer.Id}/status",
            content: null);

        toggleResponse.EnsureSuccessStatusCode();
        (await customerClient.GetAsync("/api/v1/account/profile")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false
            });
}
