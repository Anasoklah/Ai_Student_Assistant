using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Features.Documents.Dtos;

namespace SyrianStudyBot.Features.Documents.Mappers;

public static class DocumentMappers
{
    public static DocumentIngestionResultDto MapDocument(Document document) => new()
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
}
