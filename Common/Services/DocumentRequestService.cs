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
            Pages = request.Pages
        };
    }
}
