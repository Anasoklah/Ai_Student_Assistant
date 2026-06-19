using System.Text.Json;

namespace SyrianStudyBot.Domain;

public class QuizSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string? Subject { get; set; }
    public string? GradeLevel { get; set; }
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
