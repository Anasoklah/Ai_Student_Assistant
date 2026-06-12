namespace SyrianStudyBot.Dtos;

public record ExtractedPageDto
{
    public int PageNumber { get; init; }
    public string Text { get; init; } = string.Empty;
}
