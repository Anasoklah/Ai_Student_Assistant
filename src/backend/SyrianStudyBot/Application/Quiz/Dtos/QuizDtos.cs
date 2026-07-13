using System.Text.Json;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Application.Quiz.Dtos;

/// <summary>
/// Request to generate a new quiz. Supports filtering by subject, grade level,
/// document, chapter, section, or page range to scope the quiz content.
/// </summary>
public class GenerateQuizRequestDto
{
    /// <summary>Academic subject for the quiz questions.</summary>
    public Subject Subject { get; init; }

    /// <summary>Grade level filter, or null to use any grade level.</summary>
    public GradeLevel? GradeLevel { get; init; }

    /// <summary>Number of questions to generate (default 5).</summary>
    public int TotalQuestions { get; init; } = 5;

    /// <summary>Optional document ID to scope questions to a specific document.</summary>
    public Guid? DocumentId { get; init; }

    /// <summary>Optional chapter ID to scope questions to a specific chapter.</summary>
    public Guid? ChapterId { get; init; }

    /// <summary>Optional section ID to scope questions to a specific section.</summary>
    public Guid? SectionId { get; init; }

    /// <summary>Start of the page range filter (1-based, inclusive).</summary>
    public int? PageStart { get; init; }

    /// <summary>End of the page range filter (1-based, inclusive).</summary>
    public int? PageEnd { get; init; }
}

/// <summary>
/// Response representing a quiz session with its questions and score state.
/// </summary>
public class QuizSessionResponseDto
{
    /// <summary>Unique identifier of the quiz session.</summary>
    public Guid Id { get; init; }

    /// <summary>Subject the quiz covers, or null if not subject-specific.</summary>
    public Subject? Subject { get; init; }

    /// <summary>Grade level the quiz targets, or null if not grade-specific.</summary>
    public GradeLevel? GradeLevel { get; init; }

    /// <summary>Total number of questions in the quiz.</summary>
    public int TotalQuestions { get; init; }

    /// <summary>JSON-serialized list of generated questions.</summary>
    public JsonElement Questions { get; init; }

    /// <summary>JSON-serialized user answers, or null if the quiz has not been submitted.</summary>
    public JsonElement? Answers { get; init; }

    /// <summary>User's score after submission, or null if pending.</summary>
    public int? Score { get; init; }

    /// <summary>Maximum possible score for this quiz.</summary>
    public int? MaxScore { get; init; }

    /// <summary>Whether the user has submitted and completed this quiz.</summary>
    public bool IsCompleted { get; init; }

    /// <summary>UTC timestamp when the quiz was created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>UTC timestamp when the quiz was completed, or null if pending.</summary>
    public DateTime? CompletedAt { get; init; }
}

/// <summary>
/// Request to submit answers for a quiz session.
/// </summary>
public class SubmitQuizRequestDto
{
    /// <summary>JSON-serialized answers keyed by question index.</summary>
    public JsonElement Answers { get; init; }

    /// <summary>Computed score for the submission.</summary>
    public int Score { get; init; }

    /// <summary>Maximum possible score.</summary>
    public int MaxScore { get; init; }
}

/// <summary>
/// Response representing the result of a completed quiz.
/// </summary>
public class QuizResultResponseDto
{
    /// <summary>Unique identifier of the quiz result record.</summary>
    public Guid Id { get; init; }

    /// <summary>ID of the quiz session this result belongs to.</summary>
    public Guid QuizSessionId { get; init; }

    /// <summary>Subject the quiz covered.</summary>
    public Subject Subject { get; init; }

    /// <summary>User's score.</summary>
    public int Score { get; init; }

    /// <summary>Maximum possible score.</summary>
    public int MaxScore { get; init; }

    /// <summary>Score as a percentage (0.0 to 100.0).</summary>
    public double Percentage { get; init; }

    /// <summary>UTC timestamp when the quiz was completed.</summary>
    public DateTime CompletedAt { get; init; }
}
