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

        var chunks = BuildChunksFromExtractionResults(requestDto.Pages);
        logger.LogInformation("Built {Count} chunks from extraction results", chunks.Count);

        var embeddings = await embeddingService.GenerateEmbeddingsAsync(
            chunks.Select(c => c.Text).ToList(), cancellationToken);
        logger.LogInformation("Generated {Count} embeddings", embeddings.Count);

        for (var i = 0; i < chunks.Count; i++)
        {
            document.Chunks.Add(new DocumentChunk
            {
                ChunkIndex = i,
                PageNumber = chunks[i].PageNumber,
                ChapterTitle = chunks[i].ChapterTitle,
                SectionTitle = chunks[i].SectionTitle,
                StartWord = chunks[i].StartWord,
                EndWord = chunks[i].EndWord,
                Content = chunks[i].Text,
                Embedding = new Vector(embeddings[i])
            });
        }

        db.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Saved document '{Title}' with {Count} chunks to database", document.Title, document.Chunks.Count);
        return document;
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
