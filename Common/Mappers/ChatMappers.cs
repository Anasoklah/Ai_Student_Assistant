using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.Common.Mappers;

public static class ChatMappers
{
    public static ChatSessionResponseDto MapSession(ChatSession session) => new()
    {
        Id = session.Id,
        Title = session.Title,
        Subject = session.Subject,
        SectionFilter = session.SectionFilter,
        ChapterFilter = session.ChapterFilter,
        IsActive = session.IsActive,
        CreatedAt = session.CreatedAt,
        LastActiveAt = session.LastActiveAt
    };

    public static ChatMessageResponseDto MapMessage(ChatMessage message) => new()
    {
        Id = message.Id,
        Role = message.Role,
        Content = message.Content,
        SourcesJson = message.SourcesJson,
        Timestamp = message.Timestamp
    };
}
