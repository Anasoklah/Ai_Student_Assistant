using Pgvector;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Features.Documents.Dtos;
using SyrianStudyBot.Infrastructure.Persistence.Repositories;

namespace SyrianStudyBot.Features.contracts.repositories;

/// <summary>
/// Repository for all Document-related database operations.
/// Covers: Document, DocumentChunk, BookChapter, BookSection.
///
/// Every class that previously injected AppDbContext for document/chapter/section
/// queries should now use this interface instead.
/// </summary>
public interface IDocumentRepository
{
    // ── Document queries ──

    /// <summary>
    /// Returns a single document by its ID, including its chunks and chapters.
    /// </summary>
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns a paginated list of documents uploaded by a specific user.
    /// Ordered by most recent upload first.
    /// </summary>
    Task<EntityPage<Document>> GetUserDocumentsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Returns a paginated list of ALL documents (admin view).
    /// Ordered by most recent upload first.
    /// </summary>
    Task<EntityPage<Document>> GetAllDocumentsAsync(int page, int pageSize, CancellationToken ct = default);

    // ── Document commands ──

    /// <summary>
    /// Stages a new Document entity for insertion. Does not save immediately;
    /// call SaveChangesAsync to persist.
    /// </summary>
    void Add(Document document);

    // ── DocumentChunk commands ──

    /// <summary>
    /// Stages a new DocumentChunk for insertion.
    /// </summary>
    void AddChunk(DocumentChunk chunk);

    // ── Book structure commands ──

    /// <summary>
    /// Persists the extracted book structure (chapters and sections) for a document.
    /// Creates BookChapter and BookSection entities, computes page ranges,
    /// saves to the database, and returns a page→structure lookup dictionary
    /// for mapping chunks to their chapter/section.
    /// </summary>
    /// <returns>
    /// Dictionary mapping page_number → (ChapterId, SectionId, ChapterTitle, SectionTitle).
    /// Returns null if structure is null or empty.
    /// </returns>
    Task<Dictionary<int, (Guid ChapterId, Guid? SectionId, string ChapterTitle, string? SectionTitle)>>?
        SaveBookStructureAsync(Guid documentId, BookStructureDto structure, CancellationToken ct = default);

    // ── Vector search ──

    /// <summary>
    /// Performs cosine similarity search on DocumentChunk embeddings.
    /// Returns the top-K most relevant chunks, optionally filtered by
    /// subject, document, chapter, section, and page range.
    /// </summary>
    Task<List<DocumentChunk>> SearchChunksAsync(
        Pgvector.Vector embedding,
        int topK,
        Subject? subject = null,
        Guid? documentId = null,
        Guid? chapterId = null,
        Guid? sectionId = null,
        int? pageStart = null,
        int? pageEnd = null,
        CancellationToken ct = default);

    // ── Unit of Work ──

    /// <summary>
    /// Persists all tracked entity changes in the current DbContext scope.
    /// Since all repositories share the same scoped DbContext, this saves
    /// changes from ALL repositories in the current request.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
