// Base DTO with common fields
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Features.Documents.Dtos;

public class DocumentDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public Subject Subject { get; init; }
    public GradeLevel? GradeLevel { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string? Language { get; init; }
    public DocumentType DocumentType { get; init; }
}

// Admin DTO extends base with admin-only fields
public class AdminDocumentDto : DocumentDto
{
    public string? Edition { get; init; }
    public long FileSizeBytes { get; init; }
    public Guid? UploadedByUserId { get; init; }
    public DateTime UploadedAt { get; init; }
    public int ChapterCount { get; init; }
}

// Only for ingestion - keep it separate as it carries pages
public class DocumentIngestionCommand
{
    public string Title { get; init; } = string.Empty;
    public Subject Subject { get; init; }
    public GradeLevel? GradeLevel { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string? Edition { get; init; }
    public string? Language { get; init; }
    public DocumentType DocumentType { get; init; } = Domain.Enums.DocumentType.OfficialBook;
    public Guid UploadedByUserId { get; init; }
    public long FileSizeBytes { get; init; }
    public IReadOnlyList<ExtractedPageDto> Pages { get; init; } = [];
}

// Keep UploadDocumentRequest as is - it's the API contract with IFormFile