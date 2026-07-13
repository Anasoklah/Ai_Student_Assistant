using System.Text.Json;
using SyrianStudyBot.Application.Quiz.Mappers;
using SyrianStudyBot.Infrastructure.Common;
using SyrianStudyBot.Application.Common;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Domain.Exceptions;
using SyrianStudyBot.Application.Quiz.Dtos;
using SyrianStudyBot.Application.Chat;
using SyrianStudyBot.Application.Documents;
using SyrianStudyBot.Application.Payments;
using SyrianStudyBot.Application.Quiz;
using SyrianStudyBot.Application.Auth;
using SyrianStudyBot.Application.Rag;

namespace SyrianStudyBot.Application.Quiz;

/// <summary>
/// Orchestrates quiz operations: generating quizzes from RAG,
/// tracking history, and submitting results.
/// Relies on IQuizRepository for all database operations.
/// </summary>
public class QuizUseCase : IQuizUseCase
{
    private readonly IQuizRepository _quizRepo;
    private readonly IRagPipelineService _ragPipeline;

    public QuizUseCase(IQuizRepository quizRepo, IRagPipelineService ragPipeline)
    {
        _quizRepo = quizRepo;
        _ragPipeline = ragPipeline;
    }

    public async Task<QuizSessionResponseDto> GenerateQuizAsync(Guid userId, GenerateQuizRequestDto request, CancellationToken cancellationToken = default)
    {
        var totalQuestions = Math.Clamp(request.TotalQuestions, 1, 20);
        var prompt = $"Generate {totalQuestions} exam questions for {request.Subject}.";
        var quizText = await _ragPipeline.QueryAsync(
            prompt, ChatMode.Quiz, request.Subject,
            request.DocumentId, request.ChapterId, request.SectionId,
            request.PageStart, request.PageEnd,
            cancellationToken);

        var questions = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            content = quizText,
            generatedAt = DateTime.UtcNow
        }));

        var session = new QuizSession
        {
            UserId = userId,
            Subject = request.Subject,
            GradeLevel = request.GradeLevel,
            TotalQuestions = totalQuestions,
            Questions = questions
        };

        _quizRepo.AddQuiz(session);
        await _quizRepo.SaveChangesAsync(cancellationToken);

        return QuizMappers.MapSession(session);
    }

    public async Task<PagedResponse<QuizSessionResponseDto>> GetHistoryAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var entityPage = await _quizRepo.GetUserQuizzesAsync(userId, page, pageSize, cancellationToken);

        return new PagedResponse<QuizSessionResponseDto>(
            entityPage.Items.Select(q => QuizMappers.MapSession(q)).ToList(),
            entityPage.Page,
            entityPage.PageSize,
            entityPage.TotalCount);
    }

    public async Task<QuizSessionResponseDto?> GetQuizAsync(Guid userId, Guid quizSessionId, CancellationToken cancellationToken = default)
    {
        var session = await _quizRepo.GetQuizByIdAsync(quizSessionId, userId, cancellationToken);

        return session is null ? null : QuizMappers.MapSession(session);
    }

    public async Task<QuizResultResponseDto?> SubmitQuizAsync(Guid userId, Guid quizSessionId, SubmitQuizRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.MaxScore <= 0 || request.Score < 0 || request.Score > request.MaxScore)
            throw new BadRequestException("Invalid score");

        var session = await _quizRepo.GetQuizWithResultAsync(quizSessionId, userId, cancellationToken);

        if (session is null)
            return null;

        if (session.IsCompleted)
            throw new ConflictException("Quiz is already completed");

        session.Answers = JsonDocument.Parse(request.Answers.GetRawText());
        session.Score = request.Score;
        session.MaxScore = request.MaxScore;
        session.IsCompleted = true;
        session.CompletedAt = DateTime.UtcNow;

        var result = new QuizResult
        {
            UserId = userId,
            QuizSessionId = session.Id,
            Subject = session.Subject!.Value,
            Score = request.Score,
            MaxScore = request.MaxScore,
            CompletedAt = session.CompletedAt.Value
        };

        _quizRepo.AddResult(result);
        await _quizRepo.SaveChangesAsync(cancellationToken);

        return QuizMappers.MapResult(result);
    }
}
