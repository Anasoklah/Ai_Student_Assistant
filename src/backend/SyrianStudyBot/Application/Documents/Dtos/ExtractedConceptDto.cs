namespace SyrianStudyBot.Application.Documents.Dtos;

/// <summary>
/// A single concept extracted from a PDF page.
/// Each concept represents a distinct educational topic or idea with its content and keywords.
/// </summary>
public class ExtractedConceptDto
{
    /// <summary>Display title of the concept.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Detailed content or explanation of the concept.</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>Keywords associated with this concept for indexing and search.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = [];
}
