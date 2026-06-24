using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Domain.Entities;

public class ChatSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string? Title { get; set; }  // Auto-generated from first message
    public Subject? Subject { get; set; }
    public ChatMode Mode { get; set; } = ChatMode.Explain;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;

    public List<ChatMessage> Messages { get; set; } = [];
}
