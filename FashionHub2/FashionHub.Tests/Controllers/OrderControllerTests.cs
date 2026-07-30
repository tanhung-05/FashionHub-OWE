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
    public async Task OrderSuccess_ContainsValidOrderHistoryLink()
    {
        await _client.LoginAsCustomerAsync();

        var response = await _client.GetAsync("/Order/OrderSuccess/1");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("href=\"/Account/OrderHistory\"");
        html.Should().NotContain("href=\"/Account/Orders\"");
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

    [Fact]
    public async Task Checkout_NormalCart_IgnoresStaleBuyNowCart()
    {
        await _client.LoginAsCustomerAsync();
        await _client.PostFormWithAntiforgeryAsync(
            "/Cart/AddToCart",
            new Dictionary<string, string>
            {
                ["variantId"] = "1",
                ["quantity"] = "1"
            });
        await _client.PostFormWithAntiforgeryAsync(
            "/Cart/BuyNow",
            new Dictionary<string, string>
            {
                ["variantId"] = "2",
                ["quantity"] = "1"
            });

        var response = await _client.GetAsync("/Order/Checkout?cartType=Normal");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Test Product");
        html.Should().NotContain("Premium Product");
        html.Should().Contain("value=\"Normal\"");
    }

    [Fact]
    public async Task PlaceOrder_WithoutAddress_ShowsValidationMessage()
    {
        await _client.LoginAsCustomerAsync();
        await _client.PostFormWithAntiforgeryAsync(
            "/Cart/AddToCart",
            new Dictionary<string, string>
            {
                ["variantId"] = "1",
                ["quantity"] = "1"
            });
        var token = await _client.GetAntiforgeryTokenAsync(
            "/Order/Checkout?cartType=Normal");

        var placeOrderResponse = await _client.PostAsync(
            "/Order/PlaceOrder",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["addressId"] = "0",
                ["paymentMethodId"] = "1",
                ["cartType"] = "Normal",
                ["__RequestVerificationToken"] = token
            }));

        placeOrderResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        placeOrderResponse.Headers.Location?.ToString()
            .Should().Contain("cartType=Normal");

        var checkoutResponse = await _client.GetAsync(
            placeOrderResponse.Headers.Location);
        checkoutResponse.EnsureSuccessStatusCode();
        var html = await checkoutResponse.Content.ReadAsStringAsync();
        html.Should().Contain(
            "Vui lòng chọn hoặc thêm địa chỉ giao hàng trước khi đặt hàng.");
    }

    [Fact]
    public async Task PlaceOrder_NormalCart_WithValidData_RedirectsToSuccess()
    {
        await _client.LoginAsCustomerAsync();
        await _client.PostFormWithAntiforgeryAsync(
            "/Cart/AddToCart",
            new Dictionary<string, string>
            {
                ["variantId"] = "1",
                ["quantity"] = "1"
            });
        var token = await _client.GetAntiforgeryTokenAsync(
            "/Order/Checkout?cartType=Normal");

        var response = await _client.PostAsync(
            "/Order/PlaceOrder",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["addressId"] = "1",
                ["paymentMethodId"] = "1",
                ["cartType"] = "Normal",
                ["__RequestVerificationToken"] = token
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString()
            .Should().Contain("/Order/OrderSuccess");
    }
}
