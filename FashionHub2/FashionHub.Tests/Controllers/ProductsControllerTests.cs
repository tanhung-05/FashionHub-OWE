using System.Net;
using FluentAssertions;

namespace FashionHub.Tests.Controllers;

public class ProductsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;
    
    public ProductsControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task Index_ReturnsSuccessAndCorrectContentType()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/Products");
        
        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.ToString()
            .Should().Contain("text/html");
    }
    
    [Fact]
    public async Task Index_WithSearchFilter_ReturnsSuccess()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/Products?search=test");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Test Product");
    }
    
    [Fact]
    public async Task Index_WithCategoryFilter_ReturnsSuccess()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/Products?category=1");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Test Product");
        content.Should().NotContain("Premium Product");
    }
    
    [Fact]
    public async Task Index_WithPriceFilter_ReturnsSuccess()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/Products?minPrice=50000&maxPrice=200000");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Test Product");
        content.Should().NotContain("Premium Product");
    }
    
    [Fact]
    public async Task Index_WithPagination_ReturnsSuccess()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/Products?page=1&pageSize=12");
        
        // Assert
        response.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Details_WithValidId_ReturnsProductDetails()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/Products/Details/1");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Test Product");
        content.Should().Contain("product-gallery-shell");
        content.Should().Contain("product-buy-panel");
        content.Should().Contain("add-to-cart-btn");
        content.Should().Contain("JSON.parse(");
        content.Should().Contain("IDBienThe");
        content.Should().Contain("SoLuongTon");
    }
    
    [Fact]
    public async Task Details_WithInvalidId_ReturnsNotFound()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/Products/Details/99999");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    // QuickView tests removed - QuickView action was never migrated from old project
    // The functionality was replaced by product details page
}
