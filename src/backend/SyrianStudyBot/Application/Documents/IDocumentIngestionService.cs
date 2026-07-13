using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Application.Documents.Dtos;

namespace SyrianStudyBot.Application.Documents;

public interface IDocumentIngestionService
{
    Task<Document> IngestAsync(DocumentIngestionCommand requestDto, CancellationToken cancellationToken = default);

    Task AttachExtractedContentAsync(
        Document document,
        IReadOnlyList<ExtractedPageDto> pages,
        BookStructureDto? structure,
        CancellationToken cancellationToken = default);
}
