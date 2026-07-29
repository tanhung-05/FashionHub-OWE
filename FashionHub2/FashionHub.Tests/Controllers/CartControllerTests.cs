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
        var content = new FormUrlEncodedContent(formData);
        
        // Act
        var response = await _client.PostAsync("/Cart/AddToCart", content);
        
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
        var content = new FormUrlEncodedContent(formData);
        
        // Act
        var response = await _client.PostAsync("/Cart/AddToCart", content);
        
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
        await _client.PostAsync("/Cart/AddToCart", new FormUrlEncodedContent(addData));
        
        var updateData = new Dictionary<string, string>
        {
            { "variantId", "1" },
            { "quantity", "2" }
        };
        var content = new FormUrlEncodedContent(updateData);
        
        // Act
        var response = await _client.PostAsync("/Cart/UpdateCart", content);
        
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
        await _client.PostAsync("/Cart/AddToCart", new FormUrlEncodedContent(addData));
        
        // Act
        var response = await _client.PostAsync("/Cart/RemoveFromCart", 
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "variantId", "1" }
            }));
        
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
        await _client.PostAsync("/Cart/AddToCart", new FormUrlEncodedContent(addData));
        
        // Act
        var response = await _client.GetAsync("/Cart/GetCartItemCount");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var count = await response.Content.ReadAsStringAsync();
        count.Should().Contain("2");
    }
}