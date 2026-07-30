using FashionHub.Web.Services;

namespace FashionHub.Web.Application.Chat;

public sealed class ChatService : IChatService
{
    private readonly IChatContextProvider contextProvider;
    private readonly IChatConversationStore conversationStore;
    private readonly IChatAiService chatAiService;
    private readonly ILogger<ChatService> logger;

    public ChatService(
        IChatContextProvider contextProvider,
        IChatConversationStore conversationStore,
        IChatAiService chatAiService,
        ILogger<ChatService> logger)
    {
        this.contextProvider = contextProvider;
        this.conversationStore = conversationStore;
        this.chatAiService = chatAiService;
        this.logger = logger;
    }

    public async Task<ChatResponseDto> SendMessageAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        message = (message ?? string.Empty).Trim();
        if (message.Length == 0 || message.Length > ChatLimits.MaxMessageLength)
        {
            throw new ArgumentException(
                $"Message length must be between 1 and {ChatLimits.MaxMessageLength}.",
                nameof(message));
        }

        var conversation = await conversationStore.GetCurrentAsync(cancellationToken);
        var grounding = await contextProvider.GetContextAsync(
            message,
            conversation.Messages,
            cancellationToken);
        var userMessage = new ChatMessageDto(
            "user",
            message,
            DateTime.UtcNow,
            [],
            null,
            []);
        await conversationStore.AppendAsync(userMessage, cancellationToken);

        var responseText = grounding.SafeResponse;
        var isFallback = false;

        if (grounding.UseAi)
        {
            try
            {
                var aiRequest = new ChatAiRequest(
                    message,
                    grounding.SerializedContext,
                    grounding.SafeResponse,
                    conversation.Messages
                        .TakeLast(ChatLimits.MaxHistoryMessagesForAi)
                        .Select(item => new ChatAiHistoryMessage(item.Role, item.Content))
                        .ToList());
                var aiResponse = await chatAiService.GenerateReplyAsync(
                    aiRequest,
                    cancellationToken);

                if (ChatText.LooksLikeSensitiveAiOutput(aiResponse))
                {
                    logger.LogWarning(
                        "Gemini response was rejected by the sensitive-output guard.");
                    isFallback = true;
                }
                else
                {
                    responseText = aiResponse;
                }
            }
            catch (ChatAiUnavailableException exception)
            {
                logger.LogInformation(
                    "Using grounded chat fallback: {Reason}",
                    exception.Message);
                isFallback = true;
            }
        }

        responseText = responseText.Trim();
        if (responseText.Length > ChatLimits.MaxAssistantLength)
        {
            responseText = responseText[..ChatLimits.MaxAssistantLength].TrimEnd()
                + "…";
        }

        var assistantMessage = new ChatMessageDto(
            "assistant",
            responseText,
            DateTime.UtcNow,
            grounding.Products,
            grounding.Order,
            grounding.Actions);
        await conversationStore.AppendAsync(assistantMessage, cancellationToken);

        var updated = await conversationStore.GetCurrentAsync(cancellationToken);
        return new ChatResponseDto(updated.Id, assistantMessage, isFallback);
    }

    public Task<ChatConversationDto> GetCurrentConversationAsync(
        CancellationToken cancellationToken = default)
    {
        return conversationStore.GetCurrentAsync(cancellationToken);
    }

    public Task<ChatConversationDto> StartNewConversationAsync(
        CancellationToken cancellationToken = default)
    {
        return conversationStore.StartNewAsync(cancellationToken);
    }

    public Task ClearCurrentConversationAsync(
        CancellationToken cancellationToken = default)
    {
        return conversationStore.ClearCurrentAsync(cancellationToken);
    }
}
