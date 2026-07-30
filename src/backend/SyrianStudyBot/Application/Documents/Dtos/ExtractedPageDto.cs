namespace SyrianStudyBot.Application.Documents.Dtos;

/// <summary>
/// Represents the extraction result for a single PDF page.
/// Contains the raw text and structured concepts extracted by the AI service.
/// </summary>
public record ExtractedPageDto
{
    /// <summary>1-based page number within the PDF.</summary>
    public int PageNumber { get; init; }

    /// <summary>Full text content extracted from the page.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>List of structured concepts identified on this page.</summary>
    public IReadOnlyList<ExtractedConceptDto> Concepts { get; init; } = [];

    /// <summary>
    /// True when no provider passed the AI service's quality validation and a
    /// best-effort result was stored. Chunks built from this page inherit the
    /// flag so low-confidence content can be reviewed or filtered later.
    /// </summary>
    public bool NeedsReview { get; init; }
}
