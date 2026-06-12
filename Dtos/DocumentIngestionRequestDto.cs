namespace SyrianStudyBot.Dtos;

public class DocumentIngestionRequestDto
{
    public string Title { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string SourceName { get; init; } = string.Empty;
    public string? Edition { get; init; }
    public string? Language { get; init; }

    public IReadOnlyList<ExtractedPageDto> Pages { get; init; } = [];
}
