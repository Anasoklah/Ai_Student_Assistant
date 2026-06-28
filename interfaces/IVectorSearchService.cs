using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.interfaces;

public interface IVectorSearchService
{
    // Finds the most relevant document chunks for a given question vector.
    // subject: optional filter — null means search all subjects.
    // sectionFilter: optional free-text filter on SectionTitle or ChapterTitle (case-insensitive contains).
    // topK: how many chunks to return (usually 3-5).
    Task<List<DocumentChunk>> SearchAsync(
        float[] queryVector,
        Subject? subject,
        int topK,
        string? ChapterFilter = null,
        string? sectionFilter = null,
        CancellationToken cancellationToken = default);
}
