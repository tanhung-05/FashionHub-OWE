namespace FashionHub.Web.Application.Chat;

public interface IChatService
{
    Task<ChatResponseDto> SendMessageAsync(
        string message,
        CancellationToken cancellationToken = default);

    Task<ChatConversationDto> GetCurrentConversationAsync(
        CancellationToken cancellationToken = default);

    Task<ChatConversationDto> StartNewConversationAsync(
        CancellationToken cancellationToken = default);

    Task ClearCurrentConversationAsync(
        CancellationToken cancellationToken = default);
}

public interface IChatContextProvider
{
    Task<ChatGroundingContext> GetContextAsync(
        string message,
        IReadOnlyList<ChatMessageDto> history,
        CancellationToken cancellationToken = default);
}

public interface IChatConversationStore
{
    Task<ChatConversationDto> GetCurrentAsync(
        CancellationToken cancellationToken = default);

    Task AppendAsync(
        ChatMessageDto message,
        CancellationToken cancellationToken = default);

    Task<ChatConversationDto> StartNewAsync(
        CancellationToken cancellationToken = default);

    Task ClearCurrentAsync(
        CancellationToken cancellationToken = default);
}

public interface IChatFaqProvider
{
    ChatFaqEntry? Find(string normalizedMessage);
}

public sealed record ChatFaqEntry(
    string Id,
    IReadOnlyList<string> Keywords,
    string Answer,
    string LinkLabel,
    string SourceUrl);
