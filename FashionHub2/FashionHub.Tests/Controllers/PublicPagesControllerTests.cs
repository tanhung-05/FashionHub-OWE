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
    public async Task Home_UsesVersionedCoreClientAssetsAllowedByProductionCsp()
    {
        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("https://code.jquery.com/jquery-3.7.1.min.js");
        content.Should().Contain("sha256-/JqT3SQfawRcv/BIHPThkBvs0OEvtFFmqPF/lYI/Cxo=");
        content.Should().Contain("https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js");
        content.Should().Contain("https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css");
        content.Should().NotContain("/lib/jquery/dist/jquery.min.js");
    }
}
