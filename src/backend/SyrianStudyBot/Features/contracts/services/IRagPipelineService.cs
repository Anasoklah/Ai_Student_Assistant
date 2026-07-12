using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Features.contracts.services;

public interface IRagPipelineService
{
    Task<string> QueryAsync(
        string question,
        ChatMode mode,
        Subject? subject,
        Guid? documentId = null,
        Guid? chapterId = null,
        Guid? sectionId = null,
        int? pageStart = null,
        int? pageEnd = null,
        CancellationToken cancellationToken = default);
}
