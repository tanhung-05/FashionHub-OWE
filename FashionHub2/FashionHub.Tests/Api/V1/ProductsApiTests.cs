using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FashionHub.Web.Application.Common;
using FashionHub.Web.Application.Products;
using FluentAssertions;

namespace FashionHub.Tests.Api.V1;

public class ProductsApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public ProductsApiTests(CustomWebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ReturnsPagedDtos()
    {
        var response = await client.GetAsync("/api/v1/products?pageNumber=1&pageSize=10");

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<ProductSummaryDto>>();
        page.Should().NotBeNull();
        page!.Items.Should().Contain(item => item.Id == 1);
        page.TotalItems.Should().Be(2);
    }

    [Fact]
    public async Task GetProducts_SearchWithNoMatch_ReturnsEmptyPage()
    {
        var response = await client.GetAsync("/api/v1/products?search=does-not-exist");

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<ProductSummaryDto>>();
        page!.Items.Should().BeEmpty();
        page.TotalItems.Should().Be(0);
    }

    [Fact]
    public async Task GetProducts_FilterAndSort_AreAppliedBeforePagination()
    {
        var filteredResponse = await client.GetAsync(
            "/api/v1/products?categoryId=1&minPrice=50000&maxPrice=200000");
        filteredResponse.EnsureSuccessStatusCode();
        var filtered = await filteredResponse.Content
            .ReadFromJsonAsync<PagedResult<ProductSummaryDto>>();
        filtered!.Items.Should().ContainSingle(item => item.Id == 1);

        var sortedResponse = await client.GetAsync(
            "/api/v1/products?sortBy=price&sortDirection=desc&pageSize=1");
        sortedResponse.EnsureSuccessStatusCode();
        var sorted = await sortedResponse.Content
            .ReadFromJsonAsync<PagedResult<ProductSummaryDto>>();
        sorted!.Items.Should().ContainSingle(item => item.Id == 2);
        sorted.TotalItems.Should().Be(2);
    }

    [Fact]
    public async Task GetProduct_ExistingId_ReturnsDtoWithoutEfNavigationProperties()
    {
        var response = await client.GetAsync("/api/v1/products/1");

        response.EnsureSuccessStatusCode();
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("name").GetString().Should().Be("Test Product");
        document.RootElement.TryGetProperty("idsanPhamNavigation", out _).Should().BeFalse();
        document.RootElement.GetProperty("variants").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetProduct_UnknownId_ReturnsProblemDetails()
    {
        var response = await client.GetAsync("/api/v1/products/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("status").GetInt32().Should().Be(404);
        document.RootElement.GetProperty("code").GetString().Should().Be("product-not-found");
    }
}
