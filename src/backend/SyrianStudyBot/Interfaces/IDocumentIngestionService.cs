using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Features.Documents.Dtos;

namespace SyrianStudyBot.Interfaces;

public interface IDocumentIngestionService
{
    Task<Document> IngestAsync(DocumentIngestionCommand requestDto, CancellationToken cancellationToken = default);
}
