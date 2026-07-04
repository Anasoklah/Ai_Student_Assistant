namespace SyrianStudyBot.Features.Documents.Dtos;

public record ExtractedPageDto
{
    public int PageNumber { get; init; }
    public string Text { get; init; } = string.Empty;
}
