using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Features.Chat.Dtos;

public class CreateChatSessionRequestDto
{
    public string Title { get; set; } = string.Empty;
    public Subject? Subject { get; set; }
    public string? ChapterFilter { get; set; }
    public string? SectionFilter { get; set; }
}
public class ChatSessionResponseDto
{
    public Guid Id { get; init; }
    public string? Title { get; init; }
    public Subject? Subject { get; init; }
    public ChatMode Mode { get; init; }
    public bool IsActive { get; init; }

    public string? SectionFilter { get; set; } = null;
    public string? ChapterFilter { get; set; } = null;
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
    public ChatMode ChatMode {get; init;} = ChatMode.Explain; 
}

public class AskQuestionResponseDto
{
    public string Answer { get; init; } = string.Empty;
    public ChatMessageResponseDto UserMessage { get; init; } = null!;
    public ChatMessageResponseDto AssistantMessage { get; init; } = null!;
}
