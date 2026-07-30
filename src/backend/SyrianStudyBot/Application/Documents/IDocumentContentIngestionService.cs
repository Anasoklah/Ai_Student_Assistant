using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Application.Documents.Dtos;

namespace SyrianStudyBot.Application.Documents;

/// <summary>
/// Converts extracted content into searchable chunks and embeddings.
/// </summary>
public interface IDocumentContentIngestionService
{
    Task AttachExtractedContentAsync(
        Document document,
        IReadOnlyList<ExtractedPageDto> pages,
        BookStructureDto? structure,
        CancellationToken cancellationToken = default);
}
