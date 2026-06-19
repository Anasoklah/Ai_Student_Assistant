using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Domain;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;  // Keep as string for flexibility
    public string? GradeLevel { get; set; }  // "Grade10", "Baccalaureate", etc.
    public string SourceName { get; set; } = string.Empty;
    public string? Edition { get; set; }
    public string? Language { get; set; }
    public DocumentType DocumentType { get; set; } = DocumentType.OfficialBook;

    // For student uploads
    public Guid? UploadedByUserId { get; set; }
    public ApplicationUser? UploadedByUser { get; set; }
    public bool IsApproved { get; set; } = true;  // Admin approval for student uploads
    public long FileSizeBytes { get; set; }
    public string? FilePath { get; set; }  // Storage path

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public List<DocumentChunk> Chunks { get; set; } = [];
}
