namespace SyrianStudyBot.Domain;

public class UserSession
{
    public long TelegramUserId { get; set; }
    public string? CurrentSubject { get; set; }
    public string CurrentMode { get; set; } = "explain";
    public DateTime LastActive { get; set; } = DateTime.UtcNow;
}
