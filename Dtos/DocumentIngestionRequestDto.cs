using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Dtos;

public class DocumentIngestionRequestDto
{
    public string Title { get; init; } = string.Empty;
    public Subject Subject { get; init; }
    public GradeLevel? GradeLevel { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string? Edition { get; init; }
    public string? Language { get; init; }
    public DocumentType DocumentType { get; init; } = DocumentType.OfficialBook;

    public IReadOnlyList<ExtractedPageDto> Pages { get; init; } = [];
}
