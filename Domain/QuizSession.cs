using System.Text.Json;

namespace SyrianStudyBot.Domain;

public class QuizSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long TelegramUserId { get; set; }
    public JsonDocument Questions { get; set; } = null!;
    public JsonDocument? Answers { get; set; }
    public int? Score { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
