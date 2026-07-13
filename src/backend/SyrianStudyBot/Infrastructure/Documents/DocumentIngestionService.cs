using Pgvector;
using SyrianStudyBot.Application.Documents.Mappers;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Application.Documents.Dtos;
using SyrianStudyBot.Application.Chat;
using SyrianStudyBot.Application.Documents;
using SyrianStudyBot.Application.Payments;
using SyrianStudyBot.Application.Quiz;
using SyrianStudyBot.Application.Auth;
using SyrianStudyBot.Application.Common;
using SyrianStudyBot.Application.Rag;

namespace SyrianStudyBot.Infrastructure.Documents;

/// <summary>
/// Handles document ingestion: builds text chunks from extracted pages,
/// generates embeddings, and persists everything via IDocumentRepository.
/// 
/// This service contains BUSINESS LOGIC only (chunking).
/// All database operations go through IDocumentRepository.
/// </summary>
public class DocumentIngestionService(
    IDocumentRepository docRepo,
    IEmbeddingService embeddingService,
    ILogger<DocumentIngestionService> logger) : IDocumentIngestionService
{
    private const int ChunkSize = 150;
    private const int ChunkOverlap = 20;

    public async Task<Document> IngestAsync(DocumentIngestionCommand requestDto, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting ingestion for document '{Title}' (subject: {Subject})", requestDto.Title, requestDto.Subject);

        var document = DocumentMappers.ToEntity(requestDto);

        docRepo.Add(document);
        await docRepo.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Saved document '{Title}' with ID {DocumentId}", document.Title, document.Id);

        await AttachExtractedContentAsync(document, requestDto.Pages, requestDto.Structure, cancellationToken);
        await docRepo.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Saved document '{Title}' with {ChunkCount} chunks to database", document.Title, document.Chunks.Count);
        return document;
    }

    public async Task AttachExtractedContentAsync(
        Document document,
        IReadOnlyList<ExtractedPageDto> pages,
        BookStructureDto? structure,
        CancellationToken cancellationToken = default)
    {
        Dictionary<int, (Guid ChapterId, Guid? SectionId, string ChapterTitle, string? SectionTitle)>? pageLookup = null;
        if (structure is { Chapters.Count: > 0 })
        {
            pageLookup = await docRepo.SaveBookStructureAsync(document.Id, structure, cancellationToken);
            logger.LogInformation("Saved book structure with {Count} chapters", structure.Chapters.Count);
        }

        var chunks = BuildChunksFromExtractionResults(pages);
        logger.LogInformation("Built {Count} chunks from extraction results", chunks.Count);

        var embeddings = await embeddingService.GenerateEmbeddingsAsync(
            chunks.Select(c => c.Text).ToList(), cancellationToken);
        logger.LogInformation("Generated {Count} embeddings", embeddings.Count);

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

            if (pageLookup is not null)
            {
                if (pageLookup.TryGetValue(chunks[i].PageNumber, out var mapping))
                {
                    chunk.ChapterId = mapping.ChapterId;
                    chunk.SectionId = mapping.SectionId;
                    chunk.ChapterTitle = mapping.ChapterTitle;
                    chunk.SectionTitle = mapping.SectionTitle;
                }
            }

            document.Chunks.Add(chunk);
        }
    }

    private static List<SegmentChunk> BuildChunksFromExtractionResults(IReadOnlyList<ExtractedPageDto> pages)
    {
        var chunks = new List<SegmentChunk>();

        foreach (var page in pages)
        {
            bool hasConceptChunks = false;

            foreach (var concept in page.Concepts)
            {
                var content = concept.Content?.Trim();
                if (string.IsNullOrWhiteSpace(content))
                    continue;

                hasConceptChunks = true;
                chunks.AddRange(SplitTextIntoChunks(page.PageNumber, null, null, content));
            }

            if (!hasConceptChunks && !string.IsNullOrWhiteSpace(page.Text))
            {
                chunks.AddRange(SplitTextIntoChunks(page.PageNumber, null, null, page.Text));
            }
        }

        return chunks;
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
