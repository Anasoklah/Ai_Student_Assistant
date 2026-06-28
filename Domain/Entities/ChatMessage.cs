using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public ChatSession Session { get; set; } = null!;

    public ChatMessageRole Role { get; set; } = ChatMessageRole.User;
    public string Content { get; set; } = string.Empty;

    // For assistant messages: which chunks were used
    public string? SourcesJson { get; set; }  // JSON array of {sourceId, book, page}

    // Token usage tracking (for cost monitoring)
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }

    public ChatMode Mode { get; set; } = ChatMode.Explain;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
