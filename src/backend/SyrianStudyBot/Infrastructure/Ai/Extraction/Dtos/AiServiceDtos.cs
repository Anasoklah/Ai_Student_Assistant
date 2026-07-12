using System.Text.Json.Serialization;
using SyrianStudyBot.Features.Documents.Dtos;

namespace SyrianStudyBot.Infrastructure.Ai.Extraction.Dtos;

/// <summary>
/// Response returned when a PDF extraction job is accepted by the AI service.
/// Contains the job ID used to poll for status and results.
/// </summary>
public record JobAcceptedResponse(
    [property: JsonPropertyName("job_id")] string JobId,
    [property: JsonPropertyName("book_id")] string BookId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("message")] string Message
);

/// <summary>
/// Periodic status update returned by the AI service during extraction.
/// Reports progress as pages completed out of total pages.
/// </summary>
public record JobStatusResponse(
    [property: JsonPropertyName("job_id")] string JobId,
    [property: JsonPropertyName("book_id")] string BookId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("pages_done")] int PagesDone,
    [property: JsonPropertyName("pages_total")] int PagesTotal
);

/// <summary>
/// A single concept extracted from a PDF page by the AI service.
/// Each concept has a title, content, and associated keywords for indexing.
/// </summary>
public record ExtractedConcept(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("keywords")] List<string> Keywords
);

/// <summary>
/// AI service response for a single page's extraction result.
/// Contains the list of extracted concepts, quality metadata, and error information.
/// </summary>
public record PageResult(
    [property: JsonPropertyName("page_number")] int PageNumber,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("concepts")] List<ExtractedConcept> Concepts,
    [property: JsonPropertyName("error_message")] string? ErrorMessage,
    [property: JsonPropertyName("extraction_service")] string? ExtractionService,
    [property: JsonPropertyName("text_quality_score")] double? TextQualityScore
);

/// <summary>
/// Complete result of a completed extraction job.
/// Contains all pages with their extracted concepts.
/// </summary>
public record JobResultResponse(
    [property: JsonPropertyName("job_id")] string JobId,
    [property: JsonPropertyName("book_id")] string BookId,
    [property: JsonPropertyName("pages")] List<PageResult> Pages
);

/// <summary>
/// Result of extracting a single image page. Used for on-demand image extraction
/// when a user asks a question about a specific page.
/// </summary>
public record ImageExtractionResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("page_number")] int PageNumber,
    [property: JsonPropertyName("concepts")] List<ExtractedConcept> Concepts,
    [property: JsonPropertyName("error_message")] string? ErrorMessage,
    [property: JsonPropertyName("extraction_service")] string? ExtractionService
);

/// <summary>
/// A single entry in the table of contents extracted from the document.
/// Represents either a chapter or a section with its page number and hierarchy.
/// </summary>
public record StructureTocEntry(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("page_number")] int? PageNumber,
    [property: JsonPropertyName("level")] string Level,
    [property: JsonPropertyName("parent_chapter")] string? ParentChapter
);

/// <summary>
/// Complete document structure extracted from the table of contents.
/// Contains chapters and sections organized hierarchically, plus metadata
/// about the extraction method used.
/// </summary>
public record DocumentStructureResult(
    [property: JsonPropertyName("chapters")] List<StructureTocEntry> Chapters,
    [property: JsonPropertyName("sections")] List<StructureTocEntry> Sections,
    [property: JsonPropertyName("total_entries")] int TotalEntries,
    [property: JsonPropertyName("extraction_method")] string ExtractionMethod
);

/// <summary>
/// Top-level response from the structure extraction endpoint.
/// Wraps the <see cref="DocumentStructureResult"/> with success/error information.
/// </summary>
public record StructureExtractionResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("structure")] DocumentStructureResult? Structure,
    [property: JsonPropertyName("error_message")] string? ErrorMessage
);
