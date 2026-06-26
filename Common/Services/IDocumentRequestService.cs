using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.Common.Services;

public interface IDocumentRequestService
{
    DocumentIngestionRequestDto CreateAdminRequest(DocumentIngestionRequestDto request);
    DocumentIngestionRequestDto CreateStudentRequest(DocumentIngestionRequestDto request, Guid userId);
    DocumentIngestionRequestDto CreateAdminFileRequest(
        DocumentFileUploadRequestDto request,
        IReadOnlyList<ExtractedPageDto> pages,
        StoredDocumentFile storedFile);
    DocumentIngestionRequestDto CreateStudentFileRequest(
        DocumentFileUploadRequestDto request,
        Guid userId,
        IReadOnlyList<ExtractedPageDto> pages,
        StoredDocumentFile storedFile);
}
