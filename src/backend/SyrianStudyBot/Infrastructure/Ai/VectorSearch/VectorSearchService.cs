using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using SyrianStudyBot.Infrastructure.Persistence;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Interfaces;

namespace SyrianStudyBot.Infrastructure.Ai.VectorSearch;

public class VectorSearchService(AppDbContext db, ILogger<VectorSearchService> logger) : IVectorSearchService
{
    public async Task<List<DocumentChunk>> SearchAsync(
        float[] queryVector,
        Subject? subject,
        int topK,
        Guid? documentId = null,
        Guid? chapterId = null,
        Guid? sectionId = null,
        int? pageStart = null,
        int? pageEnd = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Searching top {TopK} chunks | subject={Subject} doc={Doc} chapter={Chapter} section={Section} pages={PageStart}-{PageEnd}",
            topK, subject, documentId, chapterId, sectionId, pageStart, pageEnd);

        var vector = new Vector(queryVector);

        var query = db.DocumentChunks
            .Include(c => c.Document)
            .AsQueryable();

        if (subject.HasValue)
            query = query.Where(c => c.Document.Subject == subject.Value);

        if (documentId.HasValue && documentId.Value != Guid.Empty)
            query = query.Where(c => c.DocumentId == documentId.Value);

        if (chapterId.HasValue && chapterId.Value != Guid.Empty)
            query = query.Where(c => c.ChapterId == chapterId.Value);

        if (sectionId.HasValue && sectionId.Value != Guid.Empty)
            query = query.Where(c => c.SectionId == sectionId.Value);

        if (pageStart.HasValue)
            query = query.Where(c => c.PageNumber >= pageStart.Value);

        if (pageEnd.HasValue)
            query = query.Where(c => c.PageNumber <= pageEnd.Value);

        return await query
            .OrderBy(c => c.Embedding.CosineDistance(vector))
            .Take(topK)
            .ToListAsync(cancellationToken);
    }
}
