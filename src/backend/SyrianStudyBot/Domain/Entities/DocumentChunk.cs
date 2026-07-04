using Pgvector;

namespace SyrianStudyBot.Domain.Entities;

public class DocumentChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }

    public int? PageNumber { get; set; }
    public string? ChapterTitle { get; set; }
    public string? SectionTitle { get; set; }

    public int StartWord { get; set; }
    public int EndWord { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public Vector Embedding { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Document Document { get; set; } = null!;
}
