using System.Net;
using System.Net.Mail;
using FashionHub.Web.Application.Email;
using Microsoft.Extensions.Options;

namespace FashionHub.Web.Infrastructure.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "OWE";
}

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions options;

    public SmtpEmailSender(IOptions<SmtpOptions> options)
    {
        this.options = options.Value;
    }

    public async Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetUrl,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        using var message = new MailMessage
        {
            From = new MailAddress(options.FromEmail, options.FromName),
            Subject = "Đặt lại mật khẩu OWE",
            Body = BuildBody(recipientName, resetUrl, expiresAtUtc),
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(recipientEmail, recipientName));

        using var client = new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.EnableSsl,
            UseDefaultCredentials = false
        };
        if (!string.IsNullOrWhiteSpace(options.UserName))
        {
            client.Credentials =
                new NetworkCredential(options.UserName, options.Password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(options.Host)
            || string.IsNullOrWhiteSpace(options.FromEmail))
        {
            throw new InvalidOperationException(
                "Email SMTP is not configured. Configure the Email section through user secrets.");
        }
    }

    private static string BuildBody(
        string recipientName,
        string resetUrl,
        DateTime expiresAtUtc)
    {
        var safeName = WebUtility.HtmlEncode(recipientName);
        var safeUrl = WebUtility.HtmlEncode(resetUrl);
        var expiry = expiresAtUtc.ToString("HH:mm 'UTC', dd/MM/yyyy");

        return $"""
            <div style="font-family:Arial,sans-serif;max-width:560px;margin:auto;color:#181818">
              <h2>Đặt lại mật khẩu OWE</h2>
              <p>Xin chào {safeName},</p>
              <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
              <p style="margin:28px 0">
                <a href="{safeUrl}" style="background:#111;color:#fff;padding:12px 20px;text-decoration:none">
                  Đặt lại mật khẩu
                </a>
              </p>
              <p>Liên kết có hiệu lực đến {expiry} và chỉ sử dụng được một lần.</p>
              <p>Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email.</p>
            </div>
            """;
    }
}
