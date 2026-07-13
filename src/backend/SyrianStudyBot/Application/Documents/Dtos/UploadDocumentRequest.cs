using Microsoft.AspNetCore.Http;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Application.Documents.Dtos;

/// <summary>
/// API request contract for uploading a document.
/// Contains metadata fields and the file payload as <see cref="IFormFile"/>.
/// </summary>
public class UploadDocumentRequest
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

    /// <summary>First page to extract (1-based). Null means extract from the beginning.</summary>
    public int? StartPage { get; init; }

    /// <summary>Last page to extract (1-based, inclusive). Null means extract to the end.</summary>
    public int? EndPage { get; init; }

    /// <summary>
    /// Page number of the table of contents in the PDF.
    /// When provided, the AI service extracts document structure (chapters/sections)
    /// from this page to improve retrieval accuracy.
    /// </summary>
    public int? TocPage { get; init; }

    /// <summary>
    /// Optional last page of the table of contents in the PDF.
    /// When provided, the AI service extracts document structure (chapters/sections)
    /// from the range of pages [TocPage, TocPageEnd] to improve retrieval accuracy.
    /// If TocPageEnd is null, only TocPage is used for structure extraction.
    /// </summary>
    public int? TocPageEnd { get; init; }

    /// <summary>The uploaded PDF file.</summary>
    public IFormFile File { get; init; } = null!;
}
