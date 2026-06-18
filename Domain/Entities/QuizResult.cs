namespace SyrianStudyBot.Domain;

public class QuizResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid QuizSessionId { get; set; }
    public QuizSession QuizSession { get; set; } = null!;

    public string Subject { get; set; } = string.Empty;
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public double Percentage => MaxScore > 0 ? (double)Score / MaxScore * 100 : 0;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}