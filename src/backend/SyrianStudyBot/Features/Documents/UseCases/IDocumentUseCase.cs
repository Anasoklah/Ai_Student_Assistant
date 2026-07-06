using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Features.Documents.Dtos;
using SyrianStudyBot.Features.Common.Dtos;

namespace SyrianStudyBot.Features.Documents.UseCases;

public interface IDocumentUseCase
{
    Task<DocumentIngestionResultDto> IngestDocumentAsync(DocumentIngestionRequestDto request, CancellationToken cancellationToken = default);
    Task<DocumentIngestionResultDto> IngestUploadedDocumentAsync(DocumentFileUploadRequestDto request, CancellationToken cancellationToken = default);
    Task<DocumentIngestionResultDto> SetApprovalAsync(Guid documentId, bool approve, CancellationToken cancellationToken = default);
    Task<PagedResponse<DocumentIngestionResultDto>> GetApprovedDocumentsAsync(Subject? subject, GradeLevel? gradeLevel, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResponse<DocumentIngestionResultDto>> GetDocumentsForAdminAsync(bool? isApproved, int page, int pageSize, CancellationToken cancellationToken = default);
}
