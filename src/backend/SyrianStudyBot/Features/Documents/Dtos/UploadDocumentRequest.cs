using Microsoft.AspNetCore.Http;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Features.Documents.Dtos;

public class UploadDocumentRequest
{
    public string Title { get; init; } = string.Empty;
    public Subject Subject { get; init; }
    public GradeLevel? GradeLevel { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string? Edition { get; init; }
    public string? Language { get; init; }
    public int? StartPage {get;init;}
    public int? EndPage {get;init;}
    public IFormFile File { get; init; } = null!;
}
