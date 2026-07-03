using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Features.Documents.Dtos;

namespace SyrianStudyBot.Infrastructure.Documents;

public interface IDocumentRequestService
{
    DocumentIngestionRequestDto CreateAdminRequest(DocumentIngestionRequestDto request);
    DocumentIngestionRequestDto CreateStudentRequest(DocumentIngestionRequestDto request, Guid userId);
    DocumentIngestionRequestDto CreateAdminFileRequest(
        DocumentFileUploadRequestDto request,
        IReadOnlyList<ExtractedPageDto> pages);
    DocumentIngestionRequestDto CreateStudentFileRequest(
        DocumentFileUploadRequestDto request,
        Guid userId,
        IReadOnlyList<ExtractedPageDto> pages);
}
