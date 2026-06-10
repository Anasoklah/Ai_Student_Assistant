namespace SyrianStudyBot.Domain;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string? Edition { get; set; }
    public string? Language { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public List<DocumentChunk> Chunks { get; set; } = [];
}
