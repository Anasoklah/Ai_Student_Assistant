using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Application.Rag;

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
