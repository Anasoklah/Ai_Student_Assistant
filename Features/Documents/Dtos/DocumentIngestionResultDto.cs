using System;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Features.Documents.Dtos;

public class DocumentIngestionResultDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public Subject Subject { get; init; }
    public GradeLevel? GradeLevel { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string? Edition { get; init; }
    public string? Language { get; init; }
    public DocumentType DocumentType { get; init; }
    public bool IsApproved { get; init; }
    public int ChunkCount { get; init; }
}
