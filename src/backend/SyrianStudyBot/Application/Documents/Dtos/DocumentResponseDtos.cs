using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Application.Documents.Dtos;

/// <summary>
/// Base DTO representing a document with commonly exposed fields.
/// Used for read operations across the public-facing API.
/// </summary>
public class DocumentDto
{
    /// <summary>Unique identifier of the document.</summary>
    public Guid Id { get; init; }

    /// <summary>Display title of the document.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Academic subject classification.</summary>
    public Subject Subject { get; init; }

    /// <summary>Grade level the document targets, if applicable.</summary>
    public GradeLevel? GradeLevel { get; init; }

    /// <summary>Human-readable name of the document source (e.g., publisher name).</summary>
    public string SourceName { get; init; } = string.Empty;

    /// <summary>ISO language code of the document content (e.g., "ar", "en").</summary>
    public string? Language { get; init; }

    /// <summary>Category of document (official book, notes, exam, etc.).</summary>
    public DocumentType DocumentType { get; init; }

    /// <summary>Current processing status of the document.</summary>
    public DocumentStatus Status { get; init; }

    /// <summary>Human-readable message about the processing status (e.g. error details).</summary>
    public string? StatusMessage { get; init; }
}

/// <summary>
/// Lightweight DTO for polling document processing status.
/// </summary>
public class DocumentStatusDto
{
    public Guid Id { get; init; }
    public DocumentStatus Status { get; init; }
    public string? StatusMessage { get; init; }
}

/// <summary>
/// Admin-only document DTO that extends <see cref="DocumentDto"/> with
/// fields only visible to administrators (upload metadata, file size, chapter count).
/// </summary>
public class AdminDocumentDto : DocumentDto
{
    /// <summary>Edition or version of the document, if known.</summary>
    public string? Edition { get; init; }

    /// <summary>Size of the original uploaded file in bytes.</summary>
    public long FileSizeBytes { get; init; }

    /// <summary>User ID of the uploader, or null for system-uploaded documents.</summary>
    public Guid? UploadedByUserId { get; init; }

    /// <summary>UTC timestamp when the document was uploaded.</summary>
    public DateTime UploadedAt { get; init; }

    /// <summary>Number of top-level chapters extracted from the document structure.</summary>
    public int ChapterCount { get; init; }
}
