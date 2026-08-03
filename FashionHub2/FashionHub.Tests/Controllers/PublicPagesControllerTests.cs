using FluentAssertions;

namespace FashionHub.Tests.Controllers;

public class PublicPagesControllerTests
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public PublicPagesControllerTests(
        CustomWebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Blog_ReturnsEditorialPage()
    {
        var response = await client.GetAsync("/blog");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("OWE Journal");
        content.Should().Contain("journal-feature");
    }

    [Fact]
    public async Task Contact_ReturnsOwnerAndContactMethods()
    {
        var response = await client.GetAsync("/lien-he");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Lương Tấn Hùng");
        content.Should().Contain("tel:0392410917");
        content.Should().Contain("https://zalo.me/0392410917");
    }

    [Fact]
    public async Task Home_UsesAvailableLocalCoreClientAssets()
    {
        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("/lib/jquery/dist/jquery.min.js");
        content.Should().Contain("/lib/bootstrap/dist/js/bootstrap.bundle.min.js");
        content.Should().Contain("/lib/bootstrap/dist/css/bootstrap.min.css");
        content.Should().NotContain("https://code.jquery.com");

        var jqueryResponse = await client.GetAsync("/lib/jquery/dist/jquery.min.js");
        var bootstrapResponse = await client.GetAsync("/lib/bootstrap/dist/js/bootstrap.bundle.min.js");

        jqueryResponse.EnsureSuccessStatusCode();
        bootstrapResponse.EnsureSuccessStatusCode();
    }
}
