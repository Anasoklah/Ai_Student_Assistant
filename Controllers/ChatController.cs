using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Common.Mappers;
using SyrianStudyBot.Common.Services;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Dtos;
using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize(Policy = "StudentOnly")]
public class ChatController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IRagPipelineService ragPipeline,
    IPagingService pagingService,
    IUsageTrackingService usageTrackingService) : ControllerBase
{
    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreateChatSessionRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        var session = new ChatSession
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
            Subject = request.Subject,
            Mode = request.Mode
        };

        db.ChatSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ChatMappers.MapSession(session));
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

        (page, pageSize) = pagingService.NormalizePaging(page, pageSize);

        var query = db.ChatSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastActiveAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => ChatMappers.MapSession(s))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<ChatSessionResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
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

        var ownsSession = await db.ChatSessions
            .AnyAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);
        if (!ownsSession)
            return NotFound(new { message = "Chat session not found" });

        (page, pageSize) = pagingService.NormalizePaging(page, pageSize);

        var query = db.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.Timestamp);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => ChatMappers.MapMessage(m))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<ChatMessageResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    [HttpPost("sessions/{sessionId:guid}/ask")]
    public async Task<IActionResult> Ask(Guid sessionId, [FromBody] AskQuestionRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { message = "Question is required" });

        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { message = "User not authenticated" });

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Unauthorized(new { message = "User not authenticated" });

        usageTrackingService.ResetMessageCounterIfNeeded(user);
        var dailyLimit = SubscriptionRules.GetDailyMessageLimit(user.SubscriptionTier);
        if (user.MessagesToday >= dailyLimit)
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Daily message limit reached" });

        var session = await db.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId && s.IsActive, cancellationToken);
        if (session is null)
            return NotFound(new { message = "Chat session not found" });

        var question = request.Question.Trim();
        var userMessage = new ChatMessage
        {
            SessionId = session.Id,
            Role = ChatMessageRole.User,
            Content = question
        };

        db.ChatMessages.Add(userMessage);
        await db.SaveChangesAsync(cancellationToken);

        var answer = await ragPipeline.QueryAsync(question, session.Mode, session.Subject, cancellationToken);
        var assistantMessage = new ChatMessage
        {
            SessionId = session.Id,
            Role = ChatMessageRole.Assistant,
            Content = answer
        };

        db.ChatMessages.Add(assistantMessage);

        if (string.IsNullOrWhiteSpace(session.Title))
            session.Title = question.Length <= 80 ? question : question[..80];

        session.LastActiveAt = DateTime.UtcNow;
        user.MessagesToday++;
        await usageTrackingService.UpsertDailyUsageAsync(user.Id, cancellationToken);
        await userManager.UpdateAsync(user);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new AskQuestionResponseDto
        {
            Answer = answer,
            UserMessage = ChatMappers.MapMessage(userMessage),
            AssistantMessage = ChatMappers.MapMessage(assistantMessage)
        });
    }
}
