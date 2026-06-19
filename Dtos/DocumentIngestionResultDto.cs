using System;

namespace SyrianStudyBot.Dtos;

public class DocumentIngestionResultDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string SourceName { get; init; } = string.Empty;
    public string? Edition { get; init; }
    public string? Language { get; init; }
    public int ChunkCount { get; init; }
}
