using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Interfaces;

public interface IVectorSearchService
{
    Task<List<DocumentChunk>> SearchAsync(
        float[] queryVector,
        Subject? subject,
        int topK,
        string? chapterFilter = null,
        string? sectionFilter = null,
        CancellationToken cancellationToken = default);
}
