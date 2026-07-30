using FashionHub.Web.Application.Chat;
using FashionHub.Web.Services;

namespace FashionHub.Tests.Fakes;

public sealed class TestChatAiService : IChatAiService
{
    public Task<string> GenerateReplyAsync(
        ChatAiRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.FallbackResponse);
    }
}
