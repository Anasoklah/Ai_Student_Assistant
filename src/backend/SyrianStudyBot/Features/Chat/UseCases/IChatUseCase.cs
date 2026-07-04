using SyrianStudyBot.Features.Chat.Dtos;
using SyrianStudyBot.Features.Common.Dtos;

namespace SyrianStudyBot.Features.Chat.UseCases;

public interface IChatUseCase
{
    Task<ChatSessionResponseDto> CreateSessionAsync(Guid userId, CreateChatSessionRequestDto request, CancellationToken cancellationToken = default);
    Task<PagedResponse<ChatSessionResponseDto>> GetSessionsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResponse<ChatMessageResponseDto>> GetMessagesAsync(Guid userId, Guid sessionId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AskQuestionResponseDto> AskAsync(Guid userId, Guid sessionId, AskQuestionRequestDto request, CancellationToken cancellationToken = default);
}
