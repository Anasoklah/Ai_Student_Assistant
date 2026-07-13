using System.Text.Json;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Application.Quiz.Dtos;

namespace SyrianStudyBot.Application.Quiz.Mappers;

public static class QuizMappers
{
    public static QuizSessionResponseDto MapSession(QuizSession session) => new()
    {
        Id = session.Id,
        Subject = session.Subject,
        GradeLevel = session.GradeLevel,
        TotalQuestions = session.TotalQuestions,
        Questions = session.Questions.RootElement.Clone(),
        Answers = session.Answers?.RootElement.Clone(),
        Score = session.Score,
        MaxScore = session.MaxScore,
        IsCompleted = session.IsCompleted,
        CreatedAt = session.CreatedAt,
        CompletedAt = session.CompletedAt
    };

    public static QuizResultResponseDto MapResult(QuizResult result) => new()
    {
        Id = result.Id,
        QuizSessionId = result.QuizSessionId,
        Subject = result.Subject,
        Score = result.Score,
        MaxScore = result.MaxScore,
        Percentage = result.Percentage,
        CompletedAt = result.CompletedAt
    };
}
