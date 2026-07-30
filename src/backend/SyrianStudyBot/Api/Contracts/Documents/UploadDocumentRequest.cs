using Microsoft.AspNetCore.Http;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Api.Contracts.Documents;

/// <summary>
/// API request contract for uploading a document.
/// Contains metadata fields and the file payload as <see cref="IFormFile"/>.
/// </summary>
public class UploadDocumentRequest
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

    public IFormFile File { get; init; } = null!;
}
