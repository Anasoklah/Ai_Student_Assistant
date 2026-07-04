namespace SyrianStudyBot.Domain.Entities;

public class DailyUsageLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public DateTime Date { get; set; }
    public int MessageCount { get; set; } = 0;
    public int UploadCount { get; set; } = 0;
    public long TokensUsed { get; set; } = 0;
    public decimal EstimatedCost { get; set; } = 0;  // Track API spend per user
}