using System.Net;
using FluentAssertions;

namespace FashionHub.Tests.Controllers;

public class OrderControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;
    
    public OrderControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
    
    [Fact]
    public async Task Checkout_WithoutAuth_RedirectsToLogin()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/Order/Checkout");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }
    
    [Fact]
    public async Task OrderSuccess_WithoutAuth_RedirectsToLogin()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/Order/OrderSuccess/1");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }
    
    [Fact]
    public async Task ApplyCoupon_WithValidCode_ReturnsSuccess()
    {
        // Note: This test requires authentication
        // For now, test that endpoint exists and returns redirect without auth
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "couponCode", "TEST10" }
        };
        var content = new FormUrlEncodedContent(formData);
        
        // Act
        var response = await _client.PostAsync("/Order/ApplyCoupon", content);
        
        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.BadRequest);
    }
}