using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Common.Mappers;
using SyrianStudyBot.Common.Services;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Dtos;
using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Application.UseCases;

public interface IChatUseCase
{
    Task<ChatSessionResponseDto> CreateSessionAsync(Guid userId, CreateChatSessionRequestDto request, CancellationToken cancellationToken = default);
    Task<PagedResponse<ChatSessionResponseDto>> GetSessionsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResponse<ChatMessageResponseDto>> GetMessagesAsync(Guid userId, Guid sessionId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AskQuestionResponseDto> AskAsync(Guid userId, Guid sessionId, AskQuestionRequestDto request, CancellationToken cancellationToken = default);
}

public class ChatUseCase : IChatUseCase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRagPipelineService _ragPipeline;
    private readonly IPagingService _pagingService;
    private readonly IUsageTrackingService _usageTrackingService;

    public ChatUseCase(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        IRagPipelineService ragPipeline,
        IPagingService pagingService,
        IUsageTrackingService usageTrackingService)
    {
        _db = db;
        _userManager = userManager;
        _ragPipeline = ragPipeline;
        _pagingService = pagingService;
        _usageTrackingService = usageTrackingService;
    }

    public async Task<ChatSessionResponseDto> CreateSessionAsync(Guid userId, CreateChatSessionRequestDto request, CancellationToken cancellationToken = default)
    {
        var session = new ChatSession
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
            Subject = request.Subject,
        };

        _db.ChatSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        return ChatMappers.MapSession(session);
    }

    public async Task<PagedResponse<ChatSessionResponseDto>> GetSessionsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = _pagingService.NormalizePaging(page, pageSize);

        var query = _db.ChatSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastActiveAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => ChatMappers.MapSession(s))
            .ToListAsync(cancellationToken);

        return new PagedResponse<ChatSessionResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PagedResponse<ChatMessageResponseDto>> GetMessagesAsync(Guid userId, Guid sessionId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = _pagingService.NormalizePaging(page, pageSize);

        var ownsSession = await _db.ChatSessions.AnyAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);
        if (!ownsSession)
            throw new KeyNotFoundException("Chat session not found");

        var query = _db.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.Timestamp);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => ChatMappers.MapMessage(m))
            .ToListAsync(cancellationToken);

        return new PagedResponse<ChatMessageResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<AskQuestionResponseDto> AskAsync(Guid userId, Guid sessionId, AskQuestionRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new UnauthorizedAccessException("User not authenticated");

        _usageTrackingService.ResetMessageCounterIfNeeded(user);
        var dailyLimit = SubscriptionRules.GetDailyMessageLimit(user.SubscriptionTier);
        if (user.MessagesToday >= dailyLimit)
            throw new InvalidOperationException("Daily message limit reached");

        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId && s.IsActive, cancellationToken);
        if (session is null)
            throw new KeyNotFoundException("Chat session not found");

        var question = request.Question.Trim();
        var userMessage = new ChatMessage
        {
            SessionId = session.Id,
            Role = ChatMessageRole.User,
            Content = question
        };

        _db.ChatMessages.Add(userMessage);
        await _db.SaveChangesAsync(cancellationToken);

        var answer = await _ragPipeline.QueryAsync(
            question,
            request.chatMode,
            session.Subject,
            session.ChapterFilter,
            session.SectionFilter, cancellationToken);
        var assistantMessage = new ChatMessage
        {
            SessionId = session.Id,
            Role = ChatMessageRole.Assistant,
            Content = answer
        };

        _db.ChatMessages.Add(assistantMessage);

        if (string.IsNullOrWhiteSpace(session.Title))
            session.Title = question.Length <= 80 ? question : question[..80];

        session.LastActiveAt = DateTime.UtcNow;
        user.MessagesToday++;
        await _usageTrackingService.UpsertDailyUsageAsync(user.Id, cancellationToken);
        await _userManager.UpdateAsync(user);
        await _db.SaveChangesAsync(cancellationToken);

        return new AskQuestionResponseDto
        {
            Answer = answer,
            UserMessage = ChatMappers.MapMessage(userMessage),
            AssistantMessage = ChatMappers.MapMessage(assistantMessage)
        };
    }
}
