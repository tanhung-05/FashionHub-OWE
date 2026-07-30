using System.Net;
using System.Security.Cryptography;
using System.Text;
using FashionHub.Web.Application.Authentication;
using FashionHub.Web.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FashionHub.Tests.Controllers;

public sealed class PasswordResetFlowTests
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> factory;

    public PasswordResetFlowTests(CustomWebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task PasswordReset_IsGenericSingleUseAndRevokesExistingSession()
    {
        var staleSessionClient = CreateClient();
        await staleSessionClient.LoginAsCustomerAsync();

        var resetClient = CreateClient();
        var emailSender = factory.Services.GetRequiredService<TestEmailSender>();

        await RequestResetAsync(resetClient, "unknown@example.com");
        emailSender.Messages.Should().BeEmpty(
            "an unknown address must not trigger delivery");

        var requestResponse = await RequestResetAsync(
            resetClient,
            "test@example.com");
        requestResponse.Headers.Location?.ToString()
            .Should().Be("/Account/ForgotPasswordConfirmation");

        var sentEmail = emailSender.Messages.Should().ContainSingle().Subject;
        sentEmail.RecipientEmail.Should().Be("test@example.com");

        var resetUri = new Uri(sentEmail.ResetUrl);
        var rawToken = QueryHelpers.ParseQuery(resetUri.Query)["token"].ToString();
        rawToken.Should().NotBeNullOrWhiteSpace();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var storedToken = await db.DatLaiMatKhauTokens
                .AsNoTracking()
                .SingleAsync();
            storedToken.TokenHash.Should().Be(Hash(rawToken));
            storedToken.TokenHash.Should().NotBe(rawToken);
            storedToken.NgaySuDungUtc.Should().BeNull();
        }

        var resetPath = resetUri.PathAndQuery;
        var resetToken = await resetClient.GetAntiforgeryTokenAsync(resetPath);
        var resetResponse = await resetClient.PostAsync(
            "/Account/ResetPassword",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Token"] = rawToken,
                ["NewPassword"] = "NewSecure123!",
                ["ConfirmPassword"] = "NewSecure123!",
                ["__RequestVerificationToken"] = resetToken
            }));

        resetResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resetResponse.Headers.Location?.ToString()
            .Should().Be("/Account/ResetPasswordConfirmation");

        var reusedTokenResponse = await resetClient.GetAsync(resetPath);
        reusedTokenResponse.EnsureSuccessStatusCode();
        (await reusedTokenResponse.Content.ReadAsStringAsync())
            .Should().Contain("Liên kết không hợp lệ");

        var staleSessionResponse = await staleSessionClient.GetAsync("/Account/Profile");
        staleSessionResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        staleSessionResponse.Headers.Location?.ToString()
            .Should().Contain("/Account/Login");

        var oldPasswordLogin = await LoginAsync(resetClient, "Test123!");
        oldPasswordLogin.StatusCode.Should().Be(HttpStatusCode.OK);

        var newPasswordLogin = await LoginAsync(resetClient, "NewSecure123!");
        newPasswordLogin.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    private static async Task<HttpResponseMessage> RequestResetAsync(
        HttpClient client,
        string email)
    {
        var token = await client.GetAntiforgeryTokenAsync("/Account/ForgotPassword");
        return await client.PostAsync(
            "/Account/ForgotPassword",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = email,
                ["__RequestVerificationToken"] = token
            }));
    }

    private static async Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string password)
    {
        var token = await client.GetAntiforgeryTokenAsync("/Account/Login");
        return await client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "test@example.com",
                ["Password"] = password,
                ["RememberMe"] = "false",
                ["__RequestVerificationToken"] = token
            }));
    }

    private static string Hash(string token) =>
        Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
