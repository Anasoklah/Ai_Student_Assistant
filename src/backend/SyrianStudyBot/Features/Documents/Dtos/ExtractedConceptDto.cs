namespace SyrianStudyBot.Features.Documents.Dtos;

public class ExtractedConceptDto
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public IReadOnlyList<string> Keywords { get; init; } = [];
}
