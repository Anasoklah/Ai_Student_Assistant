using System.Text.Json.Serialization;
using SyrianStudyBot.Features.Documents.Dtos;

// those Dtos For the AI Extraction Service, which is used to extract concepts and structure from documents.
//  These Dtos are used to communicate with the AI Extraction Service and to represent the responses received from it.
namespace SyrianStudyBot.Infrastructure.Ai.Extraction.Dtos;

public record JobAcceptedResponse(
    [property: JsonPropertyName("job_id")] string JobId,
    [property: JsonPropertyName("book_id")] string BookId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("message")] string Message
);

public record JobStatusResponse(
    [property: JsonPropertyName("job_id")] string JobId,
    [property: JsonPropertyName("book_id")] string BookId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("pages_done")] int PagesDone,
    [property: JsonPropertyName("pages_total")] int PagesTotal
);

public record ExtractedConcept(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("keywords")] List<string> Keywords
);

public record PageResult(
    [property: JsonPropertyName("page_number")] int PageNumber,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("concepts")] List<ExtractedConcept> Concepts,
    [property: JsonPropertyName("error_message")] string? ErrorMessage,
    [property: JsonPropertyName("extraction_service")] string? ExtractionService,
    [property: JsonPropertyName("text_quality_score")] double? TextQualityScore
);

public record JobResultResponse(
    [property: JsonPropertyName("job_id")] string JobId,
    [property: JsonPropertyName("book_id")] string BookId,
    [property: JsonPropertyName("pages")] List<PageResult> Pages
);

public record ImageExtractionResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("page_number")] int PageNumber,
    [property: JsonPropertyName("concepts")] List<ExtractedConcept> Concepts,
    [property: JsonPropertyName("error_message")] string? ErrorMessage,
    [property: JsonPropertyName("extraction_service")] string? ExtractionService
);

public record StructureTocEntry(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("page_number")] int? PageNumber,
    [property: JsonPropertyName("level")] string Level,
    [property: JsonPropertyName("parent_chapter")] string? ParentChapter
);

public record DocumentStructureResult(
    [property: JsonPropertyName("chapters")] List<StructureTocEntry> Chapters,
    [property: JsonPropertyName("sections")] List<StructureTocEntry> Sections,
    [property: JsonPropertyName("total_entries")] int TotalEntries,
    [property: JsonPropertyName("extraction_method")] string ExtractionMethod
);

public record StructureExtractionResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("structure")] DocumentStructureResult? Structure,
    [property: JsonPropertyName("error_message")] string? ErrorMessage
);
