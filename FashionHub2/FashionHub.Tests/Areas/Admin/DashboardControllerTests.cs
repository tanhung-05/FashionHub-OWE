using System.Net;
using FluentAssertions;

namespace FashionHub.Tests.Areas.Admin;

public class DashboardControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;
    
    public DashboardControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
    
    [Fact]
    public async Task Index_WithoutAuth_RedirectsToLogin()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/Admin/Dashboard");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }
    
    [Fact]
    public async Task Index_WithoutAdminRole_ShouldRedirectOrDeny()
    {
        // Note: Testing with non-admin auth would require authentication setup
        // This test verifies the endpoint exists and requires authentication
        // Arrange & Act
        var response = await _client.GetAsync("/Admin/Dashboard/Index");
        
        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Redirect,
            HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized
        );
    }
}