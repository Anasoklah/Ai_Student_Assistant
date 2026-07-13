using SyrianStudyBot.Application.Documents.Dtos;

namespace SyrianStudyBot.Application.Documents;

public interface IDocumentUseCase
{
    Task<DocumentDto> IngestUploadedDocumentAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default);
    Task<DocumentStatusDto> GetDocumentStatusAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResponse<DocumentDto>> GetMyDocumentsAsync(
    int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResponse<AdminDocumentDto>> GetAllDocumentsAsync(
    int page, int pageSize, CancellationToken cancellationToken = default);
}
