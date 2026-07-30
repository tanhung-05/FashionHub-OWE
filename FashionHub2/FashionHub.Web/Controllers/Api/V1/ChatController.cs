using FashionHub.Web.Application.Chat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FashionHub.Web.Controllers.Api.V1;

[ApiController]
[EnableRateLimiting("chat")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatService chatService;

    public ChatController(IChatService chatService)
    {
        this.chatService = chatService;
    }

    /// <summary>Sends a message to the grounded OWE shopping assistant.</summary>
    [HttpPost("messages")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<ChatResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ChatResponseDto>> SendMessage(
        [FromBody] ChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        var response = await chatService.SendMessageAsync(
            request.Message,
            cancellationToken);
        return Ok(response);
    }

    /// <summary>Returns the current guest-session or customer conversation.</summary>
    [HttpGet("conversations/current")]
    [ProducesResponseType<ChatConversationDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatConversationDto>> GetCurrentConversation(
        CancellationToken cancellationToken)
    {
        return Ok(await chatService.GetCurrentConversationAsync(cancellationToken));
    }

    /// <summary>Closes the current conversation and starts an empty one.</summary>
    [HttpPost("conversations")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<ChatConversationDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ChatConversationDto>> StartConversation(
        CancellationToken cancellationToken)
    {
        var conversation = await chatService.StartNewConversationAsync(
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, conversation);
    }

    /// <summary>Deletes all messages in the current conversation.</summary>
    [HttpDelete("conversations/current")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCurrentConversation(
        CancellationToken cancellationToken)
    {
        await chatService.ClearCurrentConversationAsync(cancellationToken);
        return NoContent();
    }
}
