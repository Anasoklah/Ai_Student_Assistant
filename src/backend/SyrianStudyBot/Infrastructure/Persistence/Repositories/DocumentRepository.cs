using Pgvector;
using Pgvector.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Application.Documents.Dtos;
using SyrianStudyBot.Application.Chat;
using SyrianStudyBot.Application.Documents;
using SyrianStudyBot.Application.Payments;
using SyrianStudyBot.Application.Quiz;
using SyrianStudyBot.Application.Auth;
using SyrianStudyBot.Application.Common;

namespace SyrianStudyBot.Infrastructure.Persistence.Repositories;

/// <summary>
/// Handles all database operations for Document, DocumentChunk, BookChapter,
/// and BookSection entities. All queries and commands for these entities
/// should go through this repository.
/// </summary>
public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _db;

    public DocumentRepository(AppDbContext db)
    {
        _db = db;
    }

    // ── Document queries ──

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Documents
            .Include(d => d.Chunks)
            .Include(d => d.Chapters)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<EntityPage<Document>> GetUserDocumentsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Documents
            .Where(d => d.UploadedByUserId == userId && d.Status == DocumentStatus.Ready)
            .OrderByDescending(d => d.UploadedAt);

        return await PaginateAsync(query, page, pageSize, ct);
    }

    public async Task<EntityPage<Document>> GetAllDocumentsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Documents
            .OrderByDescending(d => d.UploadedAt);

        return await PaginateAsync(query, page, pageSize, ct);
    }

    // ── Document commands ──

    public void Add(Document document)
    {
        _db.Documents.Add(document);
    }

    // ── DocumentChunk commands ──

    public void AddChunk(DocumentChunk chunk)
    {
        _db.DocumentChunks.Add(chunk);
    }

    // ── Book structure commands ──

    public async Task<Dictionary<int, (Guid ChapterId, Guid? SectionId, string ChapterTitle, string? SectionTitle)>>?
        SaveBookStructureAsync(Guid documentId, BookStructureDto structure, CancellationToken ct = default)
    {
        if (structure is null || structure.Chapters.Count == 0)
            return null;

        // Create chapter entities
        var chapterEntities = new List<BookChapter>();
        foreach (var chapterDto in structure.Chapters)
        {
            var chapter = new BookChapter
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                Title = chapterDto.Title,
                NormalizedTitle = chapterDto.Title.Trim(),
                StartPage = chapterDto.PageNumber
            };
            chapterEntities.Add(chapter);
            _db.BookChapters.Add(chapter);
        }

        // Create section entities (linked to their parent chapter)
        foreach (var sectionDto in structure.Sections)
        {
            var parentChapter = chapterEntities.FirstOrDefault(c =>
                c.Title == sectionDto.ParentChapter);

            var section = new BookSection
            {
                Id = Guid.NewGuid(),
                ChapterId = parentChapter?.Id ?? chapterEntities.FirstOrDefault()?.Id ?? Guid.NewGuid(),
                Title = sectionDto.Title,
                NormalizedTitle = sectionDto.Title.Trim(),
                StartPage = sectionDto.PageNumber
            };
            _db.BookSections.Add(section);
        }

        await _db.SaveChangesAsync(ct);

        // Compute EndPage for each entry (next entry's StartPage - 1)
        var sortedChapters = chapterEntities.OrderBy(c => c.StartPage ?? int.MaxValue).ToList();
        var sortedSections = await _db.BookSections
            .Where(s => s.Chapter.DocumentId == documentId)
            .OrderBy(s => s.StartPage ?? int.MaxValue)
            .ToListAsync(ct);

        for (var i = 0; i < sortedChapters.Count; i++)
        {
            var nextStart = i + 1 < sortedChapters.Count
                ? sortedChapters[i + 1].StartPage
                : null;
            sortedChapters[i].EndPage = nextStart.HasValue ? nextStart.Value - 1 : null;
        }

        for (var i = 0; i < sortedSections.Count; i++)
        {
            var nextStart = i + 1 < sortedSections.Count
                ? sortedSections[i + 1].StartPage
                : null;
            sortedSections[i].EndPage = nextStart.HasValue ? nextStart.Value - 1 : null;
        }

        await _db.SaveChangesAsync(ct);

        // Build the page→structure lookup
        var pageLookup = new Dictionary<int, (Guid ChapterId, Guid? SectionId, string ChapterTitle, string? SectionTitle)>();

        foreach (var chapter in sortedChapters)
        {
            if (!chapter.StartPage.HasValue) continue;
            var endPage = chapter.EndPage ?? int.MaxValue;
            for (var page = chapter.StartPage.Value; page <= endPage; page++)
            {
                pageLookup[page] = (chapter.Id, null, chapter.Title, null);
            }
        }

        foreach (var section in sortedSections)
        {
            if (!section.StartPage.HasValue) continue;
            var endPage = section.EndPage ?? int.MaxValue;
            for (var page = section.StartPage.Value; page <= endPage; page++)
            {
                if (pageLookup.TryGetValue(page, out var existing))
                {
                    pageLookup[page] = (existing.ChapterId, section.Id, existing.ChapterTitle, section.Title);
                }
                else
                {
                    pageLookup[page] = (section.ChapterId, section.Id, string.Empty, section.Title);
                }
            }
        }

        return pageLookup;
    }

    // ── Vector search ──

    public async Task<List<DocumentChunk>> SearchChunksAsync(
        Vector embedding,
        int topK,
        Subject? subject = null,
        Guid? documentId = null,
        Guid? chapterId = null,
        Guid? sectionId = null,
        int? pageStart = null,
        int? pageEnd = null,
        CancellationToken ct = default)
    {
        var query = _db.DocumentChunks
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
            .OrderBy(c => c.Embedding.CosineDistance(embedding))
            .Take(topK)
            .ToListAsync(ct);
    }

    // ── Unit of Work ──

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _db.SaveChangesAsync(ct);
    }

    // ── Private helpers ──

    private static async Task<EntityPage<T>> PaginateAsync<T>(
        IQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new EntityPage<T>(items, totalCount, page, pageSize);
    }
}
