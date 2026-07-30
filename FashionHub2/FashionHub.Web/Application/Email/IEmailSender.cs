namespace FashionHub.Web.Application.Email;

public interface IEmailSender
{
    Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetUrl,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);
}
