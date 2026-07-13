using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Domain.Entities;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public Subject Subject { get; set; }
    public GradeLevel? GradeLevel { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string? Edition { get; set; }
    public string? Language { get; set; }
    public DocumentType DocumentType { get; set; } = DocumentType.OfficialBook;

    // Ingestion pipeline state. New uploads start as Processing; the background
    // worker flips this to Ready or Failed once extraction + chunking finishes.
    public DocumentStatus Status { get; set; } = DocumentStatus.Processing;
    public string? StatusMessage { get; set; }
    public DateTime? ProcessedAt { get; set; }

    // For student uploads
    public Guid? UploadedByUserId { get; set; }
    public ApplicationUser? UploadedByUser { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public List<DocumentChunk> Chunks { get; set; } = [];
    public List<BookChapter> Chapters { get; set; } = [];
}
