namespace SyrianStudyBot.Application.Documents.Dtos;

/// <summary>
/// Provider-independent result of extracting educational concepts from one image.
/// </summary>
public record ExtractedImageContent(
    int PageNumber,
    IReadOnlyList<ExtractedConceptDto> Concepts,
    bool NeedsReview);
