using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using SyrianStudyBot.Data;
using SyrianStudyBot.Domain;
using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Services;

public class VectorSearchService(AppDbContext db, ILogger<VectorSearchService> logger) : IVectorSearchService
{
    public async Task<List<DocumentChunk>> SearchAsync(float[] queryVector, string? subject, int topK, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Searching for top {TopK} chunks (subject filter: {Subject})", topK, subject ?? "none");

        var vector = new Vector(queryVector);

        // Query pgvector using cosine distance (<=> operator).
        // Cosine distance measures the angle between two vectors —
        // closer to 0 means more similar in meaning.
        var query = db.DocumentChunks
            .Include(c => c.Document) // also load the parent document (we need its Subject for filtering)
            .AsQueryable();

        // If a subject is specified, only search within that subject's chunks
        if (subject is not null)
            query = query.Where(c => c.Document.Subject == subject);

        var results = await query
            .OrderBy(c => c.Embedding.CosineDistance(vector)) // nearest chunks first
            .Take(topK)
            .ToListAsync(cancellationToken);

        logger.LogDebug("Found {Count} relevant chunks", results.Count);
        return results;
    }
}
