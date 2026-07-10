using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Features.Documents.Dtos;

namespace SyrianStudyBot.Features.Documents.UseCases;

public interface IDocumentUseCase
{
    Task<DocumentDto> IngestUploadedDocumentAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<DocumentDto>> GetMyDocumentsAsync(
    int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResponse<AdminDocumentDto>> GetAllDocumentsAsync(
    int page, int pageSize, CancellationToken cancellationToken = default);
}
