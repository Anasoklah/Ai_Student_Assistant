using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Application.Documents.Commands;

/// <summary>
/// Application command for a document upload. It contains no ASP.NET types.
/// </summary>
public class UploadDocumentCommand
{
    public string Title { get; init; } = string.Empty;
    public Subject Subject { get; init; }
    public GradeLevel? GradeLevel { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string? Edition { get; init; }
    public string? Language { get; init; }
    public int? StartPage { get; init; }
    public int? EndPage { get; init; }
    public int? TocPage { get; init; }
    public int? TocPageEnd { get; init; }
    public string FileName { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public Stream FileContent { get; init; } = Stream.Null;
}
