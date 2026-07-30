using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Application.Documents.Dtos;
using SyrianStudyBot.Application.Documents.Commands;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Application.Documents.Mappers;

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
        DocumentType = document.DocumentType,
        Status = document.Status,
        StatusMessage = document.StatusMessage
    };

    // Status-only view for polling
    public static DocumentStatusDto MapToStatusDto(Document document) => new()
    {
        Id = document.Id,
        Status = document.Status,
        StatusMessage = document.StatusMessage
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
        Status = document.Status,
        StatusMessage = document.StatusMessage,
        // Admin extras
        Edition = document.Edition,
        FileSizeBytes = document.FileSizeBytes,
        UploadedByUserId = document.UploadedByUserId,
        UploadedAt = document.UploadedAt,
        ChapterCount = document.Chapters.Count // Fixed: Chapters, not Chunks
    };

    public static Document MapFromUploadDocumentCommandToEntity
    (UploadDocumentCommand command , Guid userId) => new()
    {
            Title = command.Title,
            Subject = command.Subject,
            GradeLevel = command.GradeLevel,
            SourceName = command.SourceName,
            Edition = command.Edition,
            Language = command.Language,
            DocumentType = DocumentType.OfficialBook,
            UploadedByUserId = userId,
            FileSizeBytes = command.FileSizeBytes,
            Status = DocumentStatus.Processing
    };

    public static DocumentProcessingRequest CreateDocumentProcessRequest
    (string tempPath , Document document, UploadDocumentCommand command) 
    => new(
            document.Id,
            tempPath,
            command.FileName,
            command.StartPage,
            command.EndPage,
            command.TocPage,
            command.TocPageEnd,
            document.UploadedByUserId
    );

}
