using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Features.Documents.Dtos;

public class DocumentIngestionRequestDto
{
    public string Title { get; init; } = string.Empty;
    public Subject Subject { get; init; }
    public GradeLevel? GradeLevel { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string? Edition { get; init; }
    public string? Language { get; init; }
    public DocumentType DocumentType { get; init; } = DocumentType.OfficialBook;
    public Guid? UploadedByUserId { get; init; }
    public long FileSizeBytes { get; init; }
    public string? FilePath { get; init; }

    public IReadOnlyList<ExtractedPageDto> Pages { get; init; } = [];
}
