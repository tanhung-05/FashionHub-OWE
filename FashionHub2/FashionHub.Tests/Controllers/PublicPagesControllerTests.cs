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
}
