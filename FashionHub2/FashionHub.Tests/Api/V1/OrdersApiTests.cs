using System.Net;
using System.Net.Http.Json;
using FashionHub.Web.Application.Cart;
using FashionHub.Web.Application.Common;
using FashionHub.Web.Application.Orders;
using FluentAssertions;

namespace FashionHub.Tests.Api.V1;

public class OrdersApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> factory;

    public OrdersApiTests(CustomWebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Orders_WithoutAuthentication_ReturnUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateOrder_UsesServerCartAndClearsIt()
    {
        using var client = CreateClient();
        await client.LoginAsCustomerAsync();
        await client.DeleteAsync("/api/v1/cart");
        var addResponse = await client.PostAsJsonAsync(
            "/api/v1/cart/items",
            new AddCartItemRequest { VariantId = 1, Quantity = 2 });
        addResponse.EnsureSuccessStatusCode();

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/orders",
            new CreateOrderRequest
            {
                AddressId = 1,
                PaymentMethodId = 1
            });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await createResponse.Content.ReadFromJsonAsync<OrderDetailDto>();
        order!.Subtotal.Should().Be(200000);
        order.Items.Should().ContainSingle(item => item.Quantity == 2);
        var cart = await client.GetFromJsonAsync<CartDto>("/api/v1/cart");
        cart!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrders_ReturnsOnlyCurrentUsersOrders()
    {
        using var customerClient = CreateClient();
        await customerClient.LoginAsCustomerAsync();
        var customerOrders = await customerClient.GetFromJsonAsync<PagedResult<OrderSummaryDto>>(
            "/api/v1/orders");

        using var adminClient = CreateClient();
        await adminClient.LoginAsAdminAsync();
        var adminOrders = await adminClient.GetFromJsonAsync<PagedResult<OrderSummaryDto>>(
            "/api/v1/orders");

        customerOrders.Should().NotBeNull();
        adminOrders!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateOrder_EmptyCart_ReturnsConflict()
    {
        using var client = CreateClient();
        await client.LoginAsCustomerAsync();
        await client.DeleteAsync("/api/v1/cart");

        var response = await client.PostAsJsonAsync(
            "/api/v1/orders",
            new CreateOrderRequest { AddressId = 1, PaymentMethodId = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateOrder_InvalidAddress_DoesNotClearCart()
    {
        using var client = CreateClient();
        await client.LoginAsCustomerAsync();
        await client.DeleteAsync("/api/v1/cart");
        await client.PostAsJsonAsync(
            "/api/v1/cart/items",
            new AddCartItemRequest { VariantId = 1, Quantity = 1 });

        var response = await client.PostAsJsonAsync(
            "/api/v1/orders",
            new CreateOrderRequest { AddressId = 99999, PaymentMethodId = 1 });
        var cart = await client.GetFromJsonAsync<CartDto>("/api/v1/cart");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        cart!.Items.Should().ContainSingle(item => item.VariantId == 1);
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false
            });
}
