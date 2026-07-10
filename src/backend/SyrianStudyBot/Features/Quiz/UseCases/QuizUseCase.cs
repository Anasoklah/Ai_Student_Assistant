using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Features.Quiz.Mappers;
using SyrianStudyBot.Infrastructure.Common;
using SyrianStudyBot.Infrastructure.Persistence;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Domain.Exceptions;
using SyrianStudyBot.Features.Quiz.Dtos;
using SyrianStudyBot.Interfaces;

namespace SyrianStudyBot.Features.Quiz.UseCases;

public class QuizUseCase : IQuizUseCase
{
    private readonly AppDbContext _db;
    private readonly IRagPipelineService _ragPipeline;

    public QuizUseCase(AppDbContext db, IRagPipelineService ragPipeline)
    {
        _db = db;
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

        _db.QuizSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        return QuizMappers.MapSession(session);
    }

    public async Task<PagedResponse<QuizSessionResponseDto>> GetHistoryAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        

        var query = _db.QuizSessions
            .Where(q => q.UserId == userId)
            .OrderByDescending(q => q.CreatedAt);

        
        return await query
            .Select(q => QuizMappers.MapSession(q))
            .ToPagedResponseAsync(page , pageSize , cancellationToken);

    
    }

    public async Task<QuizSessionResponseDto?> GetQuizAsync(Guid userId, Guid quizSessionId, CancellationToken cancellationToken = default)
    {
        var session = await _db.QuizSessions
            .FirstOrDefaultAsync(q => q.Id == quizSessionId && q.UserId == userId, cancellationToken);

        return session is null ? null : QuizMappers.MapSession(session);
    }

    public async Task<QuizResultResponseDto?> SubmitQuizAsync(Guid userId, Guid quizSessionId, SubmitQuizRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.MaxScore <= 0 || request.Score < 0 || request.Score > request.MaxScore)
            throw new BadRequestException("Invalid score");

        var session = await _db.QuizSessions
            .Include(q => q.Result)
            .FirstOrDefaultAsync(q => q.Id == quizSessionId && q.UserId == userId, cancellationToken);

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

        _db.QuizResults.Add(result);
        await _db.SaveChangesAsync(cancellationToken);

        return QuizMappers.MapResult(result);
    }
}
