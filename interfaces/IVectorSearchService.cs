using SyrianStudyBot.Domain;

namespace SyrianStudyBot.interfaces;

public interface IVectorSearchService
{
    // Finds the most relevant document chunks for a given question vector.
    // subject: optional filter (e.g. "Math") — null means search all subjects.
    // topK: how many chunks to return (usually 3-5).
    Task<List<DocumentChunk>> SearchAsync(float[] queryVector, string? subject, int topK, CancellationToken cancellationToken = default);
}
