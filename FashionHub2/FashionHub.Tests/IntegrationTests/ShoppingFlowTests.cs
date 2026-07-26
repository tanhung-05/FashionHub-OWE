using System.Net;
using FluentAssertions;

namespace FashionHub.Tests.IntegrationTests;

public class ShoppingFlowTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public ShoppingFlowTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
    
    [Fact]
    public async Task CompleteShoppingFlow_BrowseToCart()
    {
        // 1. Browse products
        var browseResponse = await _client.GetAsync("/Products");
        browseResponse.EnsureSuccessStatusCode();
        var browseContent = await browseResponse.Content.ReadAsStringAsync();
        browseContent.Should().Contain("Test Product");
        
        // 2. View product details
        var detailsResponse = await _client.GetAsync("/Products/Details/1");
        detailsResponse.EnsureSuccessStatusCode();
        var detailsContent = await detailsResponse.Content.ReadAsStringAsync();
        detailsContent.Should().Contain("Test Product");
        
        // 3. Add to cart
        var addToCartData = new Dictionary<string, string>
        {
            { "variantId", "1" },
            { "quantity", "1" }
        };
        var addToCartResponse = await _client.PostAsync("/Cart/AddToCart", 
            new FormUrlEncodedContent(addToCartData));
        addToCartResponse.EnsureSuccessStatusCode();
        
        // 4. View cart
        var cartResponse = await _client.GetAsync("/Cart");
        cartResponse.EnsureSuccessStatusCode();
        var cartContent = await cartResponse.Content.ReadAsStringAsync();
        cartContent.Should().Contain("Giỏ hàng");
        
        // 5. Checkout requires authentication
        var checkoutResponse = await _client.GetAsync("/Order/Checkout");
        checkoutResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        checkoutResponse.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }
    
    [Fact]
    public async Task ProductSearch_ReturnsFilteredResults()
    {
        // Search for product
        var searchResponse = await _client.GetAsync("/Products?search=test");
        searchResponse.EnsureSuccessStatusCode();
        var content = await searchResponse.Content.ReadAsStringAsync();
        content.Should().Contain("Test Product");
    }
    
    [Fact]
    public async Task ProductFiltering_ByCategoryAndPrice()
    {
        // Filter by category and price range
        var filterResponse = await _client.GetAsync("/Products?category=1&minPrice=50000&maxPrice=200000");
        filterResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task CartManagement_AddUpdateRemove()
    {
        // Add item
        var addData = new Dictionary<string, string>
        {
            { "variantId", "1" },
            { "quantity", "1" }
        };
        var addResponse = await _client.PostAsync("/Cart/AddToCart", 
            new FormUrlEncodedContent(addData));
        addResponse.EnsureSuccessStatusCode();
        
        // Update quantity
        var updateData = new Dictionary<string, string>
        {
            { "variantId", "1" },
            { "quantity", "3" }
        };
        var updateResponse = await _client.PostAsync("/Cart/UpdateQuantity", 
            new FormUrlEncodedContent(updateData));
        updateResponse.EnsureSuccessStatusCode();
        
        // Verify cart count
        var countResponse = await _client.GetAsync("/Cart/GetCartCount");
        countResponse.EnsureSuccessStatusCode();
        var count = await countResponse.Content.ReadAsStringAsync();
        count.Should().Contain("3");
        
        // Remove item
        var removeResponse = await _client.PostAsync("/Cart/RemoveItem/1", null);
        removeResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task HomePage_LoadsSuccessfully()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("FashionHub");
    }
}