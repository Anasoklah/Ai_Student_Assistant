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

    /// <summary>
    /// True when the source page did not pass the AI service's deterministic
    /// quality validation and was stored as a best-effort result. Such chunks
    /// are still indexed and searchable, but flagged so they can be reviewed or
    /// filtered. Defaults to false (validated / accepted).
    /// </summary>
    public bool NeedsReview { get; set; }

    public Document Document { get; set; } = null!;

    // FK to book structure for exact GUID-based filtering
    public Guid? ChapterId { get; set; }
    public BookChapter? Chapter { get; set; }

    public Guid? SectionId { get; set; }
    public BookSection? Section { get; set; }
}
