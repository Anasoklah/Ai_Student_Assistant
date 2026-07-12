using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Features.Documents.Dtos;
using SyrianStudyBot.Infrastructure.Ai.Extraction.Dtos;

namespace SyrianStudyBot.Features.Documents.Mappers;

public static class DocumentMappers
{
    // Student/public view
    public static DocumentDto MapToStudentDto(Document document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        Subject = document.Subject,
        GradeLevel = document.GradeLevel,
        SourceName = document.SourceName,
        Language = document.Language,
        DocumentType = document.DocumentType
    };

    // Admin view - inherits all student fields
    public static AdminDocumentDto MapToAdminDto(Document document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        Subject = document.Subject,
        GradeLevel = document.GradeLevel,
        SourceName = document.SourceName,
        Language = document.Language,
        DocumentType = document.DocumentType,
        // Admin extras
        Edition = document.Edition,
        FileSizeBytes = document.FileSizeBytes,
        UploadedByUserId = document.UploadedByUserId,
        UploadedAt = document.UploadedAt,
        ChapterCount = document.Chapters.Count // Fixed: Chapters, not Chunks
    };

    public static Document ToEntity(DocumentIngestionCommand command) => new()
    {
        Title = command.Title,
        Subject = command.Subject,
        GradeLevel = command.GradeLevel,
        SourceName = command.SourceName,
        Edition = command.Edition,
        Language = command.Language,
        DocumentType = command.DocumentType,
        UploadedByUserId = command.UploadedByUserId,
        FileSizeBytes = command.FileSizeBytes
    };

    public static DocumentIngestionCommand ToIngestionCommand(
        UploadDocumentRequest request, 
        IReadOnlyList<ExtractedPageDto> pages,
        Guid uploadedByUserId,
        BookStructureDto? structure = null) => new()
    {
        Title = request.Title,
        Subject = request.Subject,
        GradeLevel = request.GradeLevel,
        SourceName = request.SourceName,
        Edition = request.Edition,
        Language = request.Language,
        DocumentType = DocumentType.OfficialBook,
        UploadedByUserId = uploadedByUserId,
        FileSizeBytes = request.File.Length,
        Pages = pages,
        Structure = structure
    };

    public static BookStructureDto ToBookStructureDto(DocumentStructureResult structure)
    {
        return new BookStructureDto
        {
            Chapters = structure.Chapters.Select(c => new BookStructureEntryDto
            {
                Title = c.Title,
                PageNumber = c.PageNumber,
                Level = c.Level,
                ParentChapter = c.ParentChapter
            }).ToList(),
            Sections = structure.Sections.Select(s => new BookStructureEntryDto
            {
                Title = s.Title,
                PageNumber = s.PageNumber,
                Level = s.Level,
                ParentChapter = s.ParentChapter
            }).ToList()
        };
    }
}