using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Features.Documents.Dtos;

namespace SyrianStudyBot.Infrastructure.Documents;

public class DocumentRequestService : IDocumentRequestService
{
    public DocumentIngestionRequestDto CreateAdminRequest(DocumentIngestionRequestDto request)
    {
        return new DocumentIngestionRequestDto
        {
            Title = request.Title,
            Subject = request.Subject,
            GradeLevel = request.GradeLevel,
            SourceName = request.SourceName,
            Edition = request.Edition,
            Language = request.Language,
            DocumentType = DocumentType.OfficialBook,
            UploadedByUserId = null,
            FileSizeBytes = request.FileSizeBytes,
            FilePath = request.FilePath,
            Pages = request.Pages
        };
    }

    public DocumentIngestionRequestDto CreateStudentRequest(DocumentIngestionRequestDto request, Guid userId)
    {
        return new DocumentIngestionRequestDto
        {
            Title = request.Title,
            Subject = request.Subject,
            GradeLevel = request.GradeLevel,
            SourceName = request.SourceName,
            Edition = request.Edition,
            Language = request.Language,
            DocumentType = DocumentType.StudentUpload,
            UploadedByUserId = userId,
            FileSizeBytes = request.FileSizeBytes,
            FilePath = request.FilePath,
            Pages = request.Pages
        };
    }

    public DocumentIngestionRequestDto CreateAdminFileRequest(
        DocumentFileUploadRequestDto request,
        IReadOnlyList<ExtractedPageDto> pages)
    {
        return new DocumentIngestionRequestDto
        {
            Title = request.Title,
            Subject = request.Subject,
            GradeLevel = request.GradeLevel,
            SourceName = request.SourceName,
            Edition = request.Edition,
            Language = request.Language,
            DocumentType = DocumentType.OfficialBook,
            UploadedByUserId = null,
            FileSizeBytes = request.File.Length,
            FilePath = null,
            Pages = pages
        };
    }

    public DocumentIngestionRequestDto CreateStudentFileRequest(
        DocumentFileUploadRequestDto request,
        Guid userId,
        IReadOnlyList<ExtractedPageDto> pages)
    {
        return new DocumentIngestionRequestDto
        {
            Title = request.Title,
            Subject = request.Subject,
            GradeLevel = request.GradeLevel,
            SourceName = request.SourceName,
            Edition = request.Edition,
            Language = request.Language,
            DocumentType = DocumentType.StudentUpload,
            UploadedByUserId = userId,
            FileSizeBytes = request.File.Length,
            FilePath = null,
            Pages = pages
        };
    }
}
