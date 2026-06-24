using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Dtos;

public class CreateChatSessionRequestDto
{
    public string? Title { get; init; }
    public Subject? Subject { get; init; }
    public ChatMode Mode { get; init; } = ChatMode.Explain;
}

public class ChatSessionResponseDto
{
    public Guid Id { get; init; }
    public string? Title { get; init; }
    public Subject? Subject { get; init; }
    public ChatMode Mode { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastActiveAt { get; init; }
}

public class ChatMessageResponseDto
{
    public Guid Id { get; init; }
    public ChatMessageRole Role { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? SourcesJson { get; init; }
    public DateTime Timestamp { get; init; }
}

public class AskQuestionRequestDto
{
    public string Question { get; init; } = string.Empty;
}

public class AskQuestionResponseDto
{
    public string Answer { get; init; } = string.Empty;
    public ChatMessageResponseDto UserMessage { get; init; } = null!;
    public ChatMessageResponseDto AssistantMessage { get; init; } = null!;
}
