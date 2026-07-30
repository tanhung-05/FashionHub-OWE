using System.Collections.Concurrent;
using FashionHub.Web.Application.Email;

namespace FashionHub.Tests;

public sealed record SentPasswordResetEmail(
    string RecipientEmail,
    string RecipientName,
    string ResetUrl,
    DateTime ExpiresAtUtc);

public sealed class TestEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentPasswordResetEmail> messages = new();

    public IReadOnlyCollection<SentPasswordResetEmail> Messages =>
        messages.ToArray();

    public Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetUrl,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        messages.Enqueue(new SentPasswordResetEmail(
            recipientEmail,
            recipientName,
            resetUrl,
            expiresAtUtc));
        return Task.CompletedTask;
    }
}
