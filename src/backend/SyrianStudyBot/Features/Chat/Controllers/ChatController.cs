using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyrianStudyBot.Features.Chat.UseCases;
using SyrianStudyBot.Infrastructure.Identity;
using SyrianStudyBot.Features.Chat.Dtos;

namespace SyrianStudyBot.Features.Chat.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize(Policy = "StudentOnly")]
public class ChatController(
    IChatUseCase chatUseCase) : ControllerBase
{
    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreateChatSessionRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        var session = await chatUseCase.CreateSessionAsync(userId, request, cancellationToken);
        return Ok(session);
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<PagedResponse<ChatSessionResponseDto>>> GetSessions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        var sessions = await chatUseCase.GetSessionsAsync(userId, page, pageSize, cancellationToken);
        return Ok(sessions);
    }

    [HttpGet("sessions/{sessionId:guid}/messages")]
    public async Task<ActionResult<PagedResponse<ChatMessageResponseDto>>> GetMessages(
        Guid sessionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        var messages = await chatUseCase.GetMessagesAsync(userId, sessionId, page, pageSize, cancellationToken);
        return Ok(messages);
    }

    [HttpPost("sessions/{sessionId:guid}/ask")]
    public async Task<IActionResult> Ask(Guid sessionId, [FromBody] AskQuestionRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { message = "Question is required" });

        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        var response = await chatUseCase.AskAsync(userId, sessionId, request, cancellationToken);
        return Ok(response);
    }
}
