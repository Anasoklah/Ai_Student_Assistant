using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using SyrianStudyBot.Infrastructure.Persistence;
using SyrianStudyBot.Domain;
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
        string? chapterFilter = null,
        string? sectionFilter = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Searching top {TopK} chunks | subject={Subject} chapter={Chapter} section={Section}",
            topK, subject, chapterFilter ?? "any", sectionFilter ?? "any");

        var vector = new Vector(queryVector);

        var query = db.DocumentChunks
            .Include(c => c.Document)
            .Where(c => c.Document.IsApproved)
            .AsQueryable();

        if (subject.HasValue)
            query = query.Where(c => c.Document.Subject == subject.Value);

        if (!string.IsNullOrWhiteSpace(chapterFilter))
        {
            var chapter = chapterFilter.Trim();
            query = query.Where(c =>
                c.ChapterTitle != null &&
                EF.Functions.ILike(c.ChapterTitle, $"%{chapter}%"));
        }

        if (!string.IsNullOrWhiteSpace(sectionFilter))
        {
            var section = sectionFilter.Trim();
            query = query.Where(c =>
                c.SectionTitle != null &&
                EF.Functions.ILike(c.SectionTitle, $"%{section}%"));
        }

        return await query
            .OrderBy(c => c.Embedding.CosineDistance(vector))
            .Take(topK)
            .ToListAsync(cancellationToken);
    }
}
