using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.Common.Services;

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
        IReadOnlyList<ExtractedPageDto> pages,
        StoredDocumentFile storedFile)
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
            FileSizeBytes = storedFile.FileSizeBytes,
            FilePath = storedFile.FilePath,
            Pages = pages
        };
    }

    public DocumentIngestionRequestDto CreateStudentFileRequest(
        DocumentFileUploadRequestDto request,
        Guid userId,
        IReadOnlyList<ExtractedPageDto> pages,
        StoredDocumentFile storedFile)
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
            FileSizeBytes = storedFile.FileSizeBytes,
            FilePath = storedFile.FilePath,
            Pages = pages
        };
    }
}
