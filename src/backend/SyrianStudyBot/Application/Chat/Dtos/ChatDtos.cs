using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Application.Chat.Dtos;

/// <summary>
/// Request to create a new chat session with an optional title.
/// </summary>
public record CreateChatSessionRequestDto
{
    /// <summary>Display title for the chat session. Generated automatically if omitted.</summary>
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// Response representing a chat session's metadata.
/// </summary>
public record ChatSessionResponseDto
{
    /// <summary>Whether the operation was successful.</summary>
    public bool sucess { get; init; }

    /// <summary>Unique identifier of the chat session.</summary>
    public Guid Id { get; init; }

    /// <summary>Display title of the session, or null if not set.</summary>
    public string? Title { get; init; }

    /// <summary>UTC timestamp when the session was created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>UTC timestamp of the last message in this session.</summary>
    public DateTime LastActiveAt { get; init; }
}

/// <summary>
/// Response representing a single chat message.
/// </summary>
public record ChatMessageResponseDto
{
    /// <summary>Whether the operation was successful.</summary>
    public bool sucess { get; init; }

    /// <summary>Unique identifier of the message.</summary>
    public Guid Id { get; init; }

    /// <summary>Role of the message sender (user or assistant).</summary>
    public ChatMessageRole Role { get; init; }

    /// <summary>Text content of the message.</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>JSON-serialized list of source references used to generate this message, or null.</summary>
    public string? SourcesJson { get; init; }

    /// <summary>UTC timestamp when the message was created.</summary>
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Request to ask a question within a chat session.
/// Supports filtering by subject, document, chapter, section, or page range.
/// </summary>
public record AskQuestionRequestDto
{
    /// <summary>The user's question text.</summary>
    public string Question { get; init; } = string.Empty;

    /// <summary>Chat mode that controls the AI response style (explain, summarize, quiz, etc.).</summary>
    public ChatMode ChatMode { get; init; } = ChatMode.Explain;

    /// <summary>Optional subject filter to narrow retrieval scope.</summary>
    public Subject? Subject { get; init; }

    /// <summary>Optional document ID to scope the question to a specific document.</summary>
    public Guid? DocumentId { get; init; }

    /// <summary>Optional chapter ID to scope the question to a specific chapter.</summary>
    public Guid? ChapterId { get; init; }

    /// <summary>Optional section ID to scope the question to a specific section.</summary>
    public Guid? SectionId { get; init; }

    /// <summary>Start of the page range filter (1-based, inclusive).</summary>
    public int? PageStart { get; init; }

    /// <summary>End of the page range filter (1-based, inclusive).</summary>
    public int? PageEnd { get; init; }
}

/// <summary>
/// Complete response to a question, including the AI answer and both user/assistant messages.
/// </summary>
public class AskQuestionResponseDto
{
    /// <summary>The AI-generated answer text.</summary>
    public string Answer { get; init; } = string.Empty;

    /// <summary>The user's message as stored in the database.</summary>
    public ChatMessageResponseDto UserMessage { get; init; } = null!;

    /// <summary>The assistant's response as stored in the database.</summary>
    public ChatMessageResponseDto AssistantMessage { get; init; } = null!;
}
