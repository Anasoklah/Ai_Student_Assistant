using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Features.contracts.services;

public interface IVectorSearchService
{
    Task<List<DocumentChunk>> SearchAsync(
        float[] queryVector,
        Subject? subject,
        int topK,
        Guid? documentId = null,
        Guid? chapterId = null,
        Guid? sectionId = null,
        int? pageStart = null,
        int? pageEnd = null,
        CancellationToken cancellationToken = default);
}
