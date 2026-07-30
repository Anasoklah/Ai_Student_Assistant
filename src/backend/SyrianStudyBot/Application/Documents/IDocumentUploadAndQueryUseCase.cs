using SyrianStudyBot.Application.Documents.Dtos;
using SyrianStudyBot.Application.Documents.Commands;

namespace SyrianStudyBot.Application.Documents;

public interface IDocumentUploadAndQueryUseCase
{
    Task<DocumentDto> UploadAsync(UploadDocumentCommand command, CancellationToken cancellationToken = default);
    Task<DocumentStatusDto> GetDocumentStatusAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResponse<DocumentDto>> GetMyDocumentsAsync(
    int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResponse<AdminDocumentDto>> GetAllDocumentsAsync(
    int page, int pageSize, CancellationToken cancellationToken = default);
}
