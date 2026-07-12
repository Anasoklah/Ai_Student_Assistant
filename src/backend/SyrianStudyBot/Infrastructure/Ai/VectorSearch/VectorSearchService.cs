using Pgvector;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Features.contracts.repositories;
using SyrianStudyBot.Features.contracts.services;

namespace SyrianStudyBot.Infrastructure.Ai.VectorSearch;

/// <summary>
/// Performs vector similarity search on DocumentChunk embeddings.
/// All database queries go through IDocumentRepository — this service
/// handles the business logic of converting search parameters to vectors
/// and delegating to the repository.
/// </summary>
public class VectorSearchService : IVectorSearchService
{
    private readonly IDocumentRepository _docRepo;
    private readonly ILogger<VectorSearchService> _logger;

    public VectorSearchService(IDocumentRepository docRepo, ILogger<VectorSearchService> logger)
    {
        _docRepo = docRepo;
        _logger = logger;
    }

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
        _logger.LogDebug(
            "Searching top {TopK} chunks | subject={Subject} doc={Doc} chapter={Chapter} section={Section} pages={PageStart}-{PageEnd}",
            topK, subject, documentId, chapterId, sectionId, pageStart, pageEnd);

        var vector = new Vector(queryVector);

        return await _docRepo.SearchChunksAsync(
            vector,
            topK,
            subject,
            documentId,
            chapterId,
            sectionId,
            pageStart,
            pageEnd,
            cancellationToken);
    }
}
