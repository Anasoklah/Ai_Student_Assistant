using System.Text.Json;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Domain.Entities;

public class QuizSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public Subject? Subject { get; set; }
    public GradeLevel? GradeLevel { get; set; }
    public int TotalQuestions { get; set; }

    public JsonDocument Questions { get; set; } = null!;  // Generated questions
    public JsonDocument? Answers { get; set; }  // Student answers
    public int? Score { get; set; }
    public int? MaxScore { get; set; }
    public bool IsCompleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public QuizResult? Result { get; set; }
}
