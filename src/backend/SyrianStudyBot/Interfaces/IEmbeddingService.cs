using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}
