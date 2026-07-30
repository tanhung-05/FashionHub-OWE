using System.ComponentModel.DataAnnotations;

namespace FashionHub.Web.Application.Chat;

public static class ChatLimits
{
    public const int MaxMessageLength = 500;
    public const int MaxAssistantLength = 2000;
    public const int MaxMessagesPerConversation = 24;
    public const int MaxHistoryMessagesForAi = 8;
    public const int MaxProducts = 6;
}

public sealed class ChatMessageRequest
{
    [Required]
    [StringLength(ChatLimits.MaxMessageLength, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;
}

public sealed record ChatProductVariantDto(
    int Id,
    string? Color,
    string? Size,
    int StockQuantity);

public sealed record ChatProductDto(
    int Id,
    string Name,
    string ImageUrl,
    decimal OriginalPrice,
    decimal Price,
    bool IsOnSale,
    string DetailUrl,
    IReadOnlyList<ChatProductVariantDto> Variants);

public sealed record ChatOrderDto(
    int Id,
    string Status,
    decimal Total,
    DateTime CreatedAt,
    bool CanCancel,
    string DetailUrl);

public sealed record ChatActionDto(string Label, string Url);

public sealed record ChatMessageDto(
    string Role,
    string Content,
    DateTime SentAt,
    IReadOnlyList<ChatProductDto> Products,
    ChatOrderDto? Order,
    IReadOnlyList<ChatActionDto> Actions);

public sealed record ChatConversationDto(
    string Id,
    bool IsPersistent,
    IReadOnlyList<ChatMessageDto> Messages);

public sealed record ChatResponseDto(
    string ConversationId,
    ChatMessageDto Message,
    bool IsFallback);

public sealed record ChatAiHistoryMessage(string Role, string Content);

public sealed record ChatAiRequest(
    string UserMessage,
    string GroundingContext,
    string FallbackResponse,
    IReadOnlyList<ChatAiHistoryMessage> History);

public enum ChatIntentKind
{
    General,
    ProductSearch,
    OrderSupport,
    Faq,
    SecurityRefusal
}

public sealed record ChatGroundingContext(
    ChatIntentKind Intent,
    string SerializedContext,
    string SafeResponse,
    IReadOnlyList<ChatProductDto> Products,
    ChatOrderDto? Order,
    IReadOnlyList<ChatActionDto> Actions,
    bool UseAi);
