using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Features.Documents.Dtos;

namespace SyrianStudyBot.Features.Documents.Mappers;

public static class DocumentMappers
{
    public static DocumentSummaryDto MapToDto(Document document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        Subject = document.Subject,
        GradeLevel = document.GradeLevel,
        SourceName = document.SourceName,
        Edition = document.Edition,
        Language = document.Language,
        DocumentType = document.DocumentType,
        IsApproved = document.IsApproved,
        ChunkCount = document.Chunks.Count
    };

    public static Document ToEntity(DocumentIngestionCommand command)=> new()
    {
        
        
            Title = command.Title,
            Subject = command.Subject,
            GradeLevel = command.GradeLevel,
            SourceName = command.SourceName,
            Edition = command.Edition,
            Language = command.Language,
            DocumentType = command.DocumentType,
            UploadedByUserId = command.UploadedByUserId,
            FileSizeBytes = command.FileSizeBytes,
            IsApproved = command.DocumentType == Domain.Enums.DocumentType.OfficialBook
        
    };

      // In DocumentMappers.cs
public static DocumentIngestionCommand ToIngestionCommand(
    UploadDocumentRequest request, 
    IReadOnlyList<ExtractedPageDto> pages,
    Guid uploadedByUserId ) => new()
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
            Pages = pages
        
    };
}
