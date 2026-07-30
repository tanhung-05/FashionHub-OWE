using FashionHub.Web.Application.Chat;

namespace FashionHub.Web.Services;

public interface IChatAiService
{
    Task<string> GenerateReplyAsync(
        ChatAiRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ChatAiUnavailableException : Exception
{
    public ChatAiUnavailableException(string message)
        : base(message)
    {
    }

    public ChatAiUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
