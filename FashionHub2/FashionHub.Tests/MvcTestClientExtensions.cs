using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace FashionHub.Tests;

internal static partial class MvcTestClientExtensions
{
    public static async Task LoginAsCustomerAsync(this HttpClient client)
    {
        var token = await client.GetAntiforgeryTokenAsync("/Account/Login");
        var response = await client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "test@example.com",
                ["Password"] = "Test123!",
                ["RememberMe"] = "false",
                ["__RequestVerificationToken"] = token
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    public static async Task<HttpResponseMessage> LoginAsAdminAsync(this HttpClient client)
    {
        var token = await client.GetAntiforgeryTokenAsync("/Account/Login");
        return await client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "admin@example.com",
                ["Password"] = "Test123!",
                ["RememberMe"] = "false",
                ["__RequestVerificationToken"] = token
            }));
    }

    public static async Task<string> GetAntiforgeryTokenAsync(
        this HttpClient client,
        string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        var match = AntiforgeryTokenRegex().Match(html);
        match.Success.Should().BeTrue($"the page {path} should render an antiforgery token");
        return WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    [GeneratedRegex(
        "name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"",
        RegexOptions.IgnoreCase)]
    private static partial Regex AntiforgeryTokenRegex();
}
