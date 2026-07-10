using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Features.Chat.Mappers;
using SyrianStudyBot.Infrastructure.Common;
using SyrianStudyBot.Infrastructure.Persistence;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Domain.Exceptions;
using SyrianStudyBot.Features.Chat.Dtos;
using SyrianStudyBot.Interfaces;

namespace SyrianStudyBot.Features.Chat.UseCases;

public class ChatUseCase : IChatUseCase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRagPipelineService _ragPipeline;
   
    private readonly IUsageTrackingService _usageTrackingService;

    public ChatUseCase(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        IRagPipelineService ragPipeline,
        IUsageTrackingService usageTrackingService)
    {
        _db = db;
        _userManager = userManager;
        _ragPipeline = ragPipeline;
        _usageTrackingService = usageTrackingService;
    }

    public async Task<ChatSessionResponseDto> CreateSessionAsync(Guid userId, CreateChatSessionRequestDto request, CancellationToken cancellationToken = default)
    {
        var session = new ChatSession
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
        };

        _db.ChatSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        return ChatMappers.ToSessionResponeDto(session , true);
    }

    public async Task<PagedResponse<ChatSessionResponseDto>> GetSessionsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {

        var query = _db.ChatSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastActiveAt);

        
        return await query
            .Select(s => ChatMappers.ToSessionResponeDto(s , true))
            .ToPagedResponseAsync(page , pageSize ,cancellationToken);

   
    }

    public async Task<PagedResponse<ChatMessageResponseDto>> GetMessagesAsync(Guid userId, Guid sessionId, int page, int pageSize, CancellationToken cancellationToken = default)
    {

        var ownsSession = await _db.ChatSessions.AnyAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);
        if (!ownsSession)
            throw new KeyNotFoundException("Chat session not found");

        var query = _db.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.Timestamp);

        return await query
            .Select(m => ChatMappers.ToMessageResponse(m , true))
            .ToPagedResponseAsync(page , pageSize , cancellationToken);

     
    }

    public async Task<AskQuestionResponseDto> AskAsync(Guid userId, Guid sessionId, AskQuestionRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new UnauthorizedAccessException("User not authenticated");

        _usageTrackingService.ResetMessageCounterIfNeeded(user);
        var dailyLimit = SubscriptionRules.GetDailyMessageLimit(user.SubscriptionTier);
        if (user.MessagesToday >= dailyLimit)
            throw new RateLimitExceededException("Daily message limit reached");

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
            request.ChatMode,
            request.Subject,
            request.DocumentId,
            request.ChapterId,
            request.SectionId,
            request.PageStart,
            request.PageEnd,
            cancellationToken);
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
            UserMessage = ChatMappers.ToMessageResponse(userMessage , true),
            AssistantMessage = ChatMappers.ToMessageResponse(assistantMessage , true)
        };
    }
}
