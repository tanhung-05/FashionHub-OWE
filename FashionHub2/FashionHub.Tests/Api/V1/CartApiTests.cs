using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FashionHub.Web.Application.Cart;
using FashionHub.Web.Controllers.Api.V1;
using FluentAssertions;

namespace FashionHub.Tests.Api.V1;

public class CartApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> factory;

    public CartApiTests(CustomWebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetCart_NewGuest_ReturnsEmptyCart()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/cart");

        response.EnsureSuccessStatusCode();
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart!.Items.Should().BeEmpty();
        cart.TotalQuantity.Should().Be(0);
    }

    [Fact]
    public async Task AddItem_WithoutCsrfToken_ReturnsBadRequest()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/cart/items",
            new AddCartItemRequest { VariantId = 1, Quantity = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddUpdateRemove_WithCsrfToken_PreservesGuestSession()
    {
        using var client = CreateClient();
        await client.RefreshCsrfTokenAsync();

        var addResponse = await client.PostAsJsonAsync(
            "/api/v1/cart/items",
            new AddCartItemRequest { VariantId = 1, Quantity = 2 });
        addResponse.EnsureSuccessStatusCode();
        var addedCart = await addResponse.Content.ReadFromJsonAsync<CartDto>();
        addedCart!.Items.Should().ContainSingle(
            item => item.VariantId == 1 && item.Quantity == 2);
        addedCart.Subtotal.Should().Be(200000);

        var updateResponse = await client.PutAsJsonAsync(
            "/api/v1/cart/items/1",
            new UpdateCartItemRequest { Quantity = 4 });
        updateResponse.EnsureSuccessStatusCode();
        var updatedCart = await updateResponse.Content.ReadFromJsonAsync<CartDto>();
        updatedCart!.TotalQuantity.Should().Be(4);

        var removeResponse = await client.DeleteAsync("/api/v1/cart/items/1");
        removeResponse.EnsureSuccessStatusCode();
        var emptyCart = await removeResponse.Content.ReadFromJsonAsync<CartDto>();
        emptyCart!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AddItem_QuantityAboveStock_ReturnsConflictProblemDetails()
    {
        using var client = CreateClient();
        await client.RefreshCsrfTokenAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/cart/items",
            new AddCartItemRequest { VariantId = 1, Quantity = 101 });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("insufficient_stock");
    }

    [Fact]
    public async Task AddItem_InvalidBody_ReturnsValidationProblemDetails()
    {
        using var client = CreateClient();
        await client.RefreshCsrfTokenAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/cart/items",
            new AddCartItemRequest { VariantId = 0, Quantity = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task AddItem_UnknownVariant_ReturnsNotFound()
    {
        using var client = CreateClient();
        await client.RefreshCsrfTokenAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/cart/items",
            new AddCartItemRequest { VariantId = 99999, Quantity = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AddItem_NonPositiveQuantity_ReturnsBadRequest(int quantity)
    {
        using var client = CreateClient();
        await client.RefreshCsrfTokenAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/cart/items",
            new AddCartItemRequest { VariantId = 1, Quantity = quantity });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Clear_RemovesAllGuestItems()
    {
        using var client = CreateClient();
        await client.RefreshCsrfTokenAsync();
        await client.PostAsJsonAsync(
            "/api/v1/cart/items",
            new AddCartItemRequest { VariantId = 1, Quantity = 1 });

        var response = await client.DeleteAsync("/api/v1/cart");

        response.EnsureSuccessStatusCode();
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart!.Items.Should().BeEmpty();
    }

    private HttpClient CreateClient()
    {
        return factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });
    }

}
