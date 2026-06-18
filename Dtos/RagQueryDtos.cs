namespace SyrianStudyBot.Dtos;

public class RagQueryRequestDto
{
    public string Question { get; init; } = string.Empty;
    public string? Mode { get; init; }
    public string? Subject { get; init; }
}

public class RagQueryResponseDto
{
    public string Answer { get; init; } = string.Empty;
}
