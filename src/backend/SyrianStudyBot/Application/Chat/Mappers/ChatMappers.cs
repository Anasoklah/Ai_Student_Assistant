using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Application.Chat.Dtos;

namespace SyrianStudyBot.Application.Chat.Mappers;

public static class ChatMappers
{
    public static ChatSessionResponseDto ToSessionResponeDto(ChatSession session , bool isSuccess) => new()
    {
        sucess = isSuccess,
        Id = session.Id,
        Title = session.Title,
        CreatedAt = session.CreatedAt,
        LastActiveAt = session.LastActiveAt
    };

    public static ChatMessageResponseDto ToMessageResponse(ChatMessage message , bool isSuccess) => new()
    {
        sucess = isSuccess,
        Id = message.Id,
        Role = message.Role,
        Content = message.Content,
        SourcesJson = message.SourcesJson,
        Timestamp = message.Timestamp
    };
}
