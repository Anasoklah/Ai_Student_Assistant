using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Features.Rag.Dtos;

public class RagQueryRequestDto
{
    public string Question { get; init; } = string.Empty;
    public ChatMode Mode { get; init; } = ChatMode.Explain;
    public Subject? Subject { get; init; }
    public string? SectionFilter { get; init; }
    public string? ChapterFilter {get; init;}
}

public class RagQueryResponseDto
{
    public string Answer { get; init; } = string.Empty;
}
