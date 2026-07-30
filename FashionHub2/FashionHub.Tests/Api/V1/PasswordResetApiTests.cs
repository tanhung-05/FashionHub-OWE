using System.Net;
using System.Net.Http.Json;
using FashionHub.Web.Application.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace FashionHub.Tests.Api.V1;

public sealed class PasswordResetApiTests
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> factory;

    public PasswordResetApiTests(CustomWebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task ForgotAndResetPassword_WithValidToken_CompletesFlow()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await client.RefreshCsrfTokenAsync();

        var forgotResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/forgot-password",
            new ForgotPasswordRequest { Email = "test@example.com" });

        forgotResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var email = factory.Services
            .GetRequiredService<TestEmailSender>()
            .Messages
            .Should()
            .ContainSingle()
            .Subject;
        var token = QueryHelpers.ParseQuery(
            new Uri(email.ResetUrl).Query)["token"].ToString();

        await client.RefreshCsrfTokenAsync();
        var resetResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/reset-password",
            new ResetPasswordRequest
            {
                Token = token,
                NewPassword = "ApiSecure123!",
                ConfirmPassword = "ApiSecure123!"
            });

        resetResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
