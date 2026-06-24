using System.Text.Json;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Dtos;

public class GenerateQuizRequestDto
{
    public Subject Subject { get; init; }
    public GradeLevel? GradeLevel { get; init; }
    public int TotalQuestions { get; init; } = 5;
}

public class QuizSessionResponseDto
{
    public Guid Id { get; init; }
    public Subject? Subject { get; init; }
    public GradeLevel? GradeLevel { get; init; }
    public int TotalQuestions { get; init; }
    public JsonElement Questions { get; init; }
    public JsonElement? Answers { get; init; }
    public int? Score { get; init; }
    public int? MaxScore { get; init; }
    public bool IsCompleted { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public class SubmitQuizRequestDto
{
    public JsonElement Answers { get; init; }
    public int Score { get; init; }
    public int MaxScore { get; init; }
}

public class QuizResultResponseDto
{
    public Guid Id { get; init; }
    public Guid QuizSessionId { get; init; }
    public Subject Subject { get; init; }
    public int Score { get; init; }
    public int MaxScore { get; init; }
    public double Percentage { get; init; }
    public DateTime CompletedAt { get; init; }
}
