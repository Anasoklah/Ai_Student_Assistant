using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Features.Documents.Dtos;

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

/// <summary>
/// Command object used internally to ingest a fully processed document.
/// Carries the extracted page data and optional book structure.
/// This is NOT an API contract — it is built by the application layer.
/// </summary>
public class DocumentIngestionCommand
{
    /// <summary>Display title of the document.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Academic subject classification.</summary>
    public Subject Subject { get; init; }

    /// <summary>Grade level the document targets, if applicable.</summary>
    public GradeLevel? GradeLevel { get; init; }

    /// <summary>Human-readable name of the document source.</summary>
    public string SourceName { get; init; } = string.Empty;

    /// <summary>Edition or version, if applicable.</summary>
    public string? Edition { get; init; }

    /// <summary>ISO language code of the document content.</summary>
    public string? Language { get; init; }

    /// <summary>Category of document (defaults to official book).</summary>
    public DocumentType DocumentType { get; init; } = Domain.Enums.DocumentType.OfficialBook;

    /// <summary>User ID of the person who uploaded the document.</summary>
    public Guid UploadedByUserId { get; init; }

    /// <summary>Size of the original uploaded file in bytes.</summary>
    public long FileSizeBytes { get; init; }

    /// <summary>List of extracted pages with their concepts and text content.</summary>
    public IReadOnlyList<ExtractedPageDto> Pages { get; init; } = [];

    /// <summary>
    /// Optional book structure extracted from the table of contents.
    /// When present, chunks are mapped to chapters and sections for targeted retrieval.
    /// </summary>
    public BookStructureDto? Structure { get; init; }
}

/// <summary>
/// Represents a single entry (chapter or section) in a book's table of contents.
/// Used to build the page-to-structure mapping during ingestion.
/// </summary>
public class BookStructureEntryDto
{
    /// <summary>Display title of the chapter or section.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Page number where this entry begins, or null if unknown.</summary>
    public int? PageNumber { get; init; }

    /// <summary>Hierarchy level: "Chapter" or "Section".</summary>
    public string Level { get; init; } = "Section";

    /// <summary>
    /// For sections only: the title of the parent chapter this section belongs to.
    /// Null for top-level chapters.
    /// </summary>
    public string? ParentChapter { get; init; }
}

/// <summary>
/// Complete book structure extracted from the document's table of contents.
/// Contains both chapters and sections, organized hierarchically.
/// </summary>
public class BookStructureDto
{
    /// <summary>Top-level chapters in the document.</summary>
    public List<BookStructureEntryDto> Chapters { get; init; } = [];

    /// <summary>Sections nested under their respective chapters.</summary>
    public List<BookStructureEntryDto> Sections { get; init; } = [];
}
