using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace FashionHub.Tests.Controllers;

public class OrderDetailFlowTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public OrderDetailFlowTests(CustomWebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false
            });
    }

    [Fact]
    public async Task OrderDetail_RendersItemAndCancelsOrder()
    {
        await client.LoginAsCustomerAsync();
        await client.PostAsync(
            "/Cart/AddToCart",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["variantId"] = "1",
                ["quantity"] = "1"
            }));
        var checkoutToken = await client.GetAntiforgeryTokenAsync(
            "/Order/Checkout?cartType=Normal");
        var placeOrderResponse = await client.PostAsync(
            "/Order/PlaceOrder",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["addressId"] = "1",
                ["paymentMethodId"] = "1",
                ["cartType"] = "Normal",
                ["__RequestVerificationToken"] = checkoutToken
            }));

        placeOrderResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = placeOrderResponse.Headers.Location?.ToString() ?? string.Empty;
        var orderIdMatch = Regex.Match(
            location,
            @"(?:OrderSuccess/|[?&]id=)(?<id>\d+)",
            RegexOptions.IgnoreCase);
        orderIdMatch.Success.Should().BeTrue();
        var orderId = int.Parse(orderIdMatch.Groups["id"].Value);

        var detailResponse = await client.GetAsync($"/Account/OrderDetail/{orderId}");
        detailResponse.EnsureSuccessStatusCode();
        var detailHtml = await detailResponse.Content.ReadAsStringAsync();
        detailHtml.Should().Contain("<strong>x1</strong>");
        detailHtml.Should().Contain("/images/products/test.jpg");
        detailHtml.Should().Contain("AppAlert.ShowError");
        detailHtml.Should().NotContain("showToast(");

        var cancelToken = await client.GetAntiforgeryTokenAsync(
            $"/Account/OrderDetail/{orderId}");
        var cancelResponse = await client.PostAsync(
            "/Account/CancelOrder",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["id"] = orderId.ToString(),
                ["reason"] = "Test cancellation",
                ["__RequestVerificationToken"] = cancelToken
            }));

        cancelResponse.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(
            await cancelResponse.Content.ReadAsStringAsync());
        payload.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();

        var cancelledDetail = await client.GetStringAsync(
            $"/Account/OrderDetail/{orderId}");
        var decodedCancelledDetail = WebUtility.HtmlDecode(cancelledDetail);
        decodedCancelledDetail.Should().Contain("Đã hủy");
        decodedCancelledDetail.Should().Contain("Hủy đơn hàng thành công.");
    }
}
