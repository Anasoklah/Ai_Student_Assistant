using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Features.Documents.Dtos;

namespace SyrianStudyBot.Infrastructure.Documents;

public interface IDocumentRequestService
{
    DocumentIngestionRequestDto CreateAdminRequest(DocumentIngestionRequestDto request);
    DocumentIngestionRequestDto CreateAdminFileRequest(
        DocumentFileUploadRequestDto request,
        IReadOnlyList<ExtractedPageDto> pages);
}
