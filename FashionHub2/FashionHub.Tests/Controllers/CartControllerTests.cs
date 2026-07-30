using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace FashionHub.Tests.Controllers;

public class CartControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;
    
    public CartControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
    
    [Fact]
    public async Task Index_ReturnsCartView()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/Cart");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Giỏ hàng");
    }
    
    [Fact]
    public async Task AddToCart_WithValidVariant_ReturnsSuccess()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "variantId", "1" },
            { "quantity", "1" }
        };
        // Act
        var response = await _client.PostFormWithAntiforgeryAsync(
            "/Cart/AddToCart",
            formData);
        
        // Assert
        response.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task AddToCart_WithInvalidVariant_ReturnsBadRequest()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "variantId", "99999" },
            { "quantity", "1" }
        };
        // Act
        var response = await _client.PostFormWithAntiforgeryAsync(
            "/Cart/AddToCart",
            formData);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task UpdateQuantity_WithValidData_ReturnsSuccess()
    {
        // Arrange - First add item to cart
        var addData = new Dictionary<string, string>
        {
            { "variantId", "1" },
            { "quantity", "1" }
        };
        await _client.PostFormWithAntiforgeryAsync("/Cart/AddToCart", addData);
        
        var updateData = new Dictionary<string, string>
        {
            { "variantId", "1" },
            { "quantity", "2" }
        };
        // Act
        var response = await _client.PostFormWithAntiforgeryAsync(
            "/Cart/UpdateCart",
            updateData);
        
        // Assert
        response.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task RemoveItem_WithValidVariant_ReturnsSuccess()
    {
        // Arrange - First add item to cart
        var addData = new Dictionary<string, string>
        {
            { "variantId", "1" },
            { "quantity", "1" }
        };
        await _client.PostFormWithAntiforgeryAsync("/Cart/AddToCart", addData);
        
        // Act
        var response = await _client.PostFormWithAntiforgeryAsync(
            "/Cart/RemoveFromCart",
            new Dictionary<string, string>
            {
                { "variantId", "1" }
            });
        
        // Assert
        response.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task GetCartCount_ReturnsCorrectCount()
    {
        // Arrange - Add item to cart
        var addData = new Dictionary<string, string>
        {
            { "variantId", "1" },
            { "quantity", "2" }
        };
        await _client.PostFormWithAntiforgeryAsync("/Cart/AddToCart", addData);
        
        // Act
        var response = await _client.GetAsync("/Cart/GetCartItemCount");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var count = await response.Content.ReadAsStringAsync();
        count.Should().Contain("2");
    }

    [Fact]
    public async Task GetCartOffcanvas_ReturnsItemsWithoutNestedOffcanvasShell()
    {
        await _client.PostFormWithAntiforgeryAsync(
            "/Cart/AddToCart",
            new Dictionary<string, string>
            {
                ["variantId"] = "1",
                ["quantity"] = "1"
            });

        var response = await _client.GetAsync("/Cart/GetCartOffcanvas");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Test Product");
        html.Should().Contain("cart-offcanvas-item");
        html.Should().NotContain("id=\"cartOffcanvas\"");
    }
}
