using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.interfaces;

public interface IVectorSearchService
{
    // Finds the most relevant document chunks for a given question vector.
    // subject: optional filter. Null means search all subjects.
    // topK: how many chunks to return (usually 3-5).
    Task<List<DocumentChunk>> SearchAsync(float[] queryVector, Subject? subject, int topK, CancellationToken cancellationToken = default);
}
