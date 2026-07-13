using Microsoft.AspNetCore.Identity;
using SyrianStudyBot.Application.Chat.Mappers;
using SyrianStudyBot.Infrastructure.Common;
using SyrianStudyBot.Application.Common;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Domain.Exceptions;
using SyrianStudyBot.Application.Chat.Dtos;
using SyrianStudyBot.Application.Chat;
using SyrianStudyBot.Application.Documents;
using SyrianStudyBot.Application.Payments;
using SyrianStudyBot.Application.Quiz;
using SyrianStudyBot.Application.Auth;
using SyrianStudyBot.Application.Rag;

namespace SyrianStudyBot.Application.Chat;

/// <summary>
/// Orchestrates chat operations: creating sessions, listing messages, and handling Q&A.
/// Relies on IChatRepository for all database operations and IRagPipelineService
/// for AI-powered retrieval-augmented generation.
/// </summary>
public class ChatUseCase : IChatUseCase
{
    private readonly IChatRepository _chatRepo;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRagPipelineService _ragPipeline;
    private readonly IUsageTrackingService _usageTrackingService;

    public ChatUseCase(
        IChatRepository chatRepo,
        UserManager<ApplicationUser> userManager,
        IRagPipelineService ragPipeline,
        IUsageTrackingService usageTrackingService)
    {
        _chatRepo = chatRepo;
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

        _chatRepo.AddSession(session);
        await _chatRepo.SaveChangesAsync(cancellationToken);

        return ChatMappers.ToSessionResponeDto(session, true);
    }

    public async Task<PagedResponse<ChatSessionResponseDto>> GetSessionsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var entityPage = await _chatRepo.GetUserSessionsAsync(userId, page, pageSize, cancellationToken);

        return new PagedResponse<ChatSessionResponseDto>(
            entityPage.Items.Select(s => ChatMappers.ToSessionResponeDto(s, true)).ToList(),
            entityPage.Page,
            entityPage.PageSize,
            entityPage.TotalCount);
    }

    public async Task<PagedResponse<ChatMessageResponseDto>> GetMessagesAsync(Guid userId, Guid sessionId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var ownsSession = await _chatRepo.SessionExistsAsync(sessionId, userId, cancellationToken);
        if (!ownsSession)
            throw new KeyNotFoundException("Chat session not found");

        var entityPage = await _chatRepo.GetSessionMessagesAsync(sessionId, page, pageSize, cancellationToken);

        return new PagedResponse<ChatMessageResponseDto>(
            entityPage.Items.Select(m => ChatMappers.ToMessageResponse(m, true)).ToList(),
            entityPage.Page,
            entityPage.PageSize,
            entityPage.TotalCount);
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

        var session = await _chatRepo.GetSessionByIdAsync(sessionId, userId, cancellationToken);
        if (session is null)
            throw new KeyNotFoundException("Chat session not found");

        var question = request.Question.Trim();
        var userMessage = new ChatMessage
        {
            SessionId = session.Id,
            Role = ChatMessageRole.User,
            Content = question
        };

        _chatRepo.AddMessage(userMessage);
        await _chatRepo.SaveChangesAsync(cancellationToken);

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

        _chatRepo.AddMessage(assistantMessage);

        if (string.IsNullOrWhiteSpace(session.Title))
            session.Title = question.Length <= 80 ? question : question[..80];

        session.LastActiveAt = DateTime.UtcNow;
        user.MessagesToday++;
        await _usageTrackingService.UpsertDailyUsageAsync(user.Id, cancellationToken);
        await _userManager.UpdateAsync(user);
        await _chatRepo.SaveChangesAsync(cancellationToken);

        return new AskQuestionResponseDto
        {
            Answer = answer,
            UserMessage = ChatMappers.ToMessageResponse(userMessage, true),
            AssistantMessage = ChatMappers.ToMessageResponse(assistantMessage, true)
        };
    }
}
