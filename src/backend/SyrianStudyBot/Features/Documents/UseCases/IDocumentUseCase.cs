using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Features.Documents.Dtos;
using SyrianStudyBot.Features.Common.Dtos;

namespace SyrianStudyBot.Features.Documents.UseCases;

public interface IDocumentUseCase
{
    Task<DocumentSummaryDto> IngestUploadedDocumentAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default);
    Task<DocumentSummaryDto> SetApprovalAsync(Guid documentId, bool approve, CancellationToken cancellationToken = default);
    Task<PagedResponse<DocumentSummaryDto>> GetApprovedDocumentsAsync(Subject? subject, GradeLevel? gradeLevel, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResponse<DocumentSummaryDto>> GetDocumentsForAdminAsync(bool? isApproved, int page, int pageSize, CancellationToken cancellationToken = default);
}
