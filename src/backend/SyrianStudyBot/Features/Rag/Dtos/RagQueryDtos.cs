using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Features.Rag.Dtos;

public class RagQueryRequestDto
{
    public string Question { get; init; } = string.Empty;
    public ChatMode Mode { get; init; } = ChatMode.Explain;
    public Subject? Subject { get; init; }
    public Guid? DocumentId { get; init; }
    public Guid? ChapterId { get; init; }
    public Guid? SectionId { get; init; }
    public int? PageStart { get; init; }
    public int? PageEnd { get; init; }
}

public class RagQueryResponseDto
{
    public string Answer { get; init; } = string.Empty;
}
