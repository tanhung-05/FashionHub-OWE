using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace FashionHub.Tests.Api.V1;

public class HealthApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> factory;

    public HealthApiTests(CustomWebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Health_WhenOptionalGeminiKeyIsMissing_ReturnsDegradedJson()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("status").GetString().Should().Be("Degraded");
        document.RootElement.GetProperty("checks").EnumerateArray()
            .Should().Contain(check =>
                check.GetProperty("name").GetString() == "GeminiAI"
                && check.GetProperty("status").GetString() == "Degraded");
    }
}
