using System.Text.RegularExpressions;
using Pgvector;
using SyrianStudyBot.Infrastructure.Persistence;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Features.Documents.Dtos;
using SyrianStudyBot.Interfaces;
using SyrianStudyBot.Features.Documents.Mappers;

namespace SyrianStudyBot.Infrastructure.Documents;

public class DocumentIngestionService(
    AppDbContext db,
    IEmbeddingService embeddingService,
    ILogger<DocumentIngestionService> logger) : IDocumentIngestionService
{
    private const int ChunkSize = 150;
    private const int ChunkOverlap = 20;

    public async Task<Document> IngestAsync(DocumentIngestionCommand requestDto, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting ingestion for document '{Title}' (subject: {Subject})", requestDto.Title, requestDto.Subject);

        var document = DocumentMappers.ToEntity(requestDto);

        // Step 1: Save document first to get its ID
        db.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Saved document '{Title}' with ID {DocumentId}", document.Title, document.Id);

        // Step 2: Save book structure if provided
        Dictionary<int, (Guid ChapterId, Guid? SectionId, string ChapterTitle, string? SectionTitle)>? pageLookup = null;
        if (requestDto.Structure is { Chapters.Count: > 0 })
        {
            pageLookup = await SaveStructureAsync(document.Id, requestDto.Structure, cancellationToken);
            logger.LogInformation("Saved {ChapterCount} chapters and {SectionCount} sections",
                requestDto.Structure.Chapters.Count, requestDto.Structure.Sections.Count);
        }

        // Step 3: Build chunks
        var chunks = BuildChunksFromExtractionResults(requestDto.Pages);
        logger.LogInformation("Built {Count} chunks from extraction results", chunks.Count);

        // Step 4: Generate embeddings
        var embeddings = await embeddingService.GenerateEmbeddingsAsync(
            chunks.Select(c => c.Text).ToList(), cancellationToken);
        logger.LogInformation("Generated {Count} embeddings", embeddings.Count);

        // Step 5: Create DocumentChunks with structure mapping
        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = new DocumentChunk
            {
                DocumentId = document.Id,
                ChunkIndex = i,
                PageNumber = chunks[i].PageNumber,
                ChapterTitle = chunks[i].ChapterTitle,
                SectionTitle = chunks[i].SectionTitle,
                StartWord = chunks[i].StartWord,
                EndWord = chunks[i].EndWord,
                Content = chunks[i].Text,
                Embedding = new Vector(embeddings[i])
            };

            // Map to structure if available
            if (pageLookup is not null)
            {
                if (pageLookup.TryGetValue(chunks[i].PageNumber, out var mapping))
                {
                    chunk.ChapterId = mapping.ChapterId;
                    chunk.SectionId = mapping.SectionId;
                    // Override heuristic titles with actual structure titles
                    chunk.ChapterTitle = mapping.ChapterTitle;
                    chunk.SectionTitle = mapping.SectionTitle;
                }
            }

            document.Chunks.Add(chunk);
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Saved document '{Title}' with {ChunkCount} chunks to database", document.Title, document.Chunks.Count);
        return document;
    }

    private async Task<Dictionary<int, (Guid ChapterId, Guid? SectionId, string ChapterTitle, string? SectionTitle)>> SaveStructureAsync(
        Guid documentId,
        BookStructureDto structure,
        CancellationToken cancellationToken)
    {
        // Build page→structure lookup
        var pageLookup = new Dictionary<int, (Guid ChapterId, Guid? SectionId, string ChapterTitle, string? SectionTitle)>();

        // Collect all entries sorted by page number
        var allEntries = structure.Chapters
            .Concat(structure.Sections)
            .Where(e => e.PageNumber.HasValue)
            .OrderBy(e => e.PageNumber!.Value)
            .ToList();

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
            db.BookChapters.Add(chapter);
        }

        // Create section entities (linked to their parent chapter)
        foreach (var sectionDto in structure.Sections)
        {
            // Find parent chapter by title match
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
            db.BookSections.Add(section);
        }

        await db.SaveChangesAsync(cancellationToken);

        // Compute EndPage for each entry (next entry's StartPage - 1)
        var sortedChapters = chapterEntities.OrderBy(c => c.StartPage ?? int.MaxValue).ToList();
        var sortedSections = db.BookSections.Where(s => s.Chapter.DocumentId == documentId)
            .OrderBy(s => s.StartPage ?? int.MaxValue).ToList();

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

        await db.SaveChangesAsync(cancellationToken);

        // Build the lookup: for each page, find which chapter and section it belongs to
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
                    // Page already has a chapter, add section info
                    pageLookup[page] = (existing.ChapterId, section.Id, existing.ChapterTitle, section.Title);
                }
                else
                {
                    // Page has a section but no chapter (shouldn't happen, but handle gracefully)
                    pageLookup[page] = (section.ChapterId, section.Id, string.Empty, section.Title);
                }
            }
        }

        logger.LogInformation("Built page lookup with {Count} entries", pageLookup.Count);
        return pageLookup;
    }

    private static List<SegmentChunk> BuildChunksFromExtractionResults(IReadOnlyList<ExtractedPageDto> pages)
    {
        var chunks = new List<SegmentChunk>();

        foreach (var page in pages)
        {
            var pageChunks = BuildChunksFromPage(page);
            if (pageChunks.Count == 0 && !string.IsNullOrWhiteSpace(page.Text))
            {
                pageChunks = SplitTextIntoChunks(page.PageNumber, null, null, page.Text);
            }

            chunks.AddRange(pageChunks);
        }

        return chunks;
    }

    private static List<SegmentChunk> BuildChunksFromPage(ExtractedPageDto page)
    {
        if (page.Concepts.Count == 0)
            return new List<SegmentChunk>();

        return page.Concepts
            .SelectMany(concept => BuildChunksFromConcept(page.PageNumber, concept))
            .ToList();
    }

    private static IEnumerable<SegmentChunk> BuildChunksFromConcept(int pageNumber, ExtractedConceptDto concept)
    {
        var content = concept.Content?.Trim();
        if (string.IsNullOrWhiteSpace(content))
            return Enumerable.Empty<SegmentChunk>();

        var chapterTitle = TryExtractChapterTitle(concept.Title);
        var sectionTitle = TryExtractSectionTitle(concept.Title);
        var title = concept.Title?.Trim();

        if (chapterTitle is not null)
            return SplitTextIntoChunks(pageNumber, chapterTitle, null, content);

        if (sectionTitle is not null)
            return SplitTextIntoChunks(pageNumber, null, sectionTitle, content);

        return SplitTextIntoChunks(pageNumber, null, title, content);
    }

    private static string? TryExtractChapterTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return null;

        return Regex.IsMatch(title.Trim(), "^(الفصل|الوحدة|الباب|المحور)", RegexOptions.IgnoreCase)
            ? title.Trim()
            : null;
    }

    private static string? TryExtractSectionTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return null;

        return Regex.IsMatch(title.Trim(), "^(الدرس|درس)", RegexOptions.IgnoreCase)
            ? title.Trim()
            : null;
    }

    private sealed record SegmentChunk(
        int PageNumber,
        string? ChapterTitle,
        string? SectionTitle,
        string Text,
        int StartWord,
        int EndWord);

    private static List<SegmentChunk> SplitTextIntoChunks(int pageNumber, string? chapterTitle, string? sectionTitle, string content)
    {
        var chunks = new List<SegmentChunk>();
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int step = ChunkSize - ChunkOverlap;
        int start = 0;

        while (start < words.Length)
        {
            var end = Math.Min(start + ChunkSize, words.Length);
            var chunkText = string.Join(' ', words[start..end]);
            chunks.Add(new SegmentChunk(
                pageNumber,
                chapterTitle,
                sectionTitle,
                chunkText,
                start,
                end - 1));
            start += step;
        }

        return chunks;
    }
}
