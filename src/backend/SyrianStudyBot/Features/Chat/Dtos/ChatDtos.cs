using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Features.Chat.Dtos;

public record CreateChatSessionRequestDto
{
    public string Title { get; set; } = string.Empty;
  
}
public record ChatSessionResponseDto
{
    public bool sucess {get;init;}
    public Guid Id { get; init; }
    public string? Title { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastActiveAt { get; init; }
}

public record ChatMessageResponseDto
{
    public bool sucess {get;init;}
    public Guid Id { get; init; }
    public ChatMessageRole Role { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? SourcesJson { get; init; }
    public DateTime Timestamp { get; init; }
}

public record AskQuestionRequestDto
{
    public string Question { get; init; } = string.Empty;
    public ChatMode ChatMode {get; init;} = ChatMode.Explain;
    public Subject? Subject {get;init;}
    public Guid? DocumentId {get; init;}
    public Guid? ChapterId {get; init;}
    public Guid? SectionId {get; init;}
    public int? PageStart {get;init;}
    public int? PageEnd {get;init;} 
}

public class AskQuestionResponseDto
{
    public string Answer { get; init; } = string.Empty;
    public ChatMessageResponseDto UserMessage { get; init; } = null!;
    public ChatMessageResponseDto AssistantMessage { get; init; } = null!;
}
