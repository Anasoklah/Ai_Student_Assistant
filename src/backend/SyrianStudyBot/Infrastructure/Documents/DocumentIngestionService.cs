using System.Text.RegularExpressions;
using Pgvector;
using SyrianStudyBot.Infrastructure.Persistence;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Features.Documents.Dtos;
using SyrianStudyBot.Interfaces;

namespace SyrianStudyBot.Infrastructure.Documents;

public class DocumentIngestionService(
    AppDbContext db,
    IEmbeddingService embeddingService,
    ILogger<DocumentIngestionService> logger) : IDocumentIngestionService
{
    private const int ChunkSize = 150;
    private const int ChunkOverlap = 20;

    public async Task<Document> IngestAsync(DocumentIngestionRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting ingestion for document '{Title}' (subject: {Subject})", requestDto.Title, requestDto.Subject);

        var document = new Document
        {
            Title = requestDto.Title,
            Subject = requestDto.Subject,
            GradeLevel = requestDto.GradeLevel,
            SourceName = requestDto.SourceName,
            Edition = requestDto.Edition,
            Language = requestDto.Language,
            DocumentType = requestDto.DocumentType,
            UploadedByUserId = requestDto.UploadedByUserId,
            FileSizeBytes = requestDto.FileSizeBytes,
            FilePath = requestDto.FilePath,
            IsApproved = requestDto.DocumentType == Domain.Enums.DocumentType.OfficialBook
        };

        var segments = SplitIntoSegments(requestDto.Pages);
        logger.LogInformation("Detected {Count} text segments across sections", segments.Count);

        var chunks = segments.SelectMany(SplitSegmentIntoChunks).ToList();
        logger.LogInformation("Split into {Count} chunks", chunks.Count);

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

    private enum HeadingLevel { None, Chapter, Section }

    private static HeadingLevel DetectHeadingLevel(string line)
    {
        if (line.StartsWith("## ")) return HeadingLevel.Section;
        if (line.StartsWith("# ") && !line.StartsWith("## ")) return HeadingLevel.Chapter;

        if (Regex.IsMatch(line, @"^(الفصل|الوحدة|الباب|المحور)\s"))
            return HeadingLevel.Chapter;

        if (Regex.IsMatch(line, @"^(الدرس|درس)\s"))
            return HeadingLevel.Section;

        return HeadingLevel.None;
    }

    private static string StripHeadingPrefix(string line)
    {
        if (line.StartsWith("## ")) return line[3..].Trim();
        if (line.StartsWith("# "))  return line[2..].Trim();
        return line.Trim();
    }

    private record TextSegment(int PageNumber, string? ChapterTitle, string? SectionTitle, string Text);

    private static List<TextSegment> SplitIntoSegments(IReadOnlyList<ExtractedPageDto> pages)
    {
        var segments = new List<TextSegment>();
        string? currentChapter = null;
        string? currentSection = null;
        var currentLines = new List<string>();
        int currentPageNumber = pages.Count > 0 ? pages[0].PageNumber : 1;

        foreach (var page in pages)
        {
            var lines = page.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var level = DetectHeadingLevel(line);

                if (level == HeadingLevel.Chapter)
                {
                    FlushSegment(segments, currentLines, currentPageNumber, currentChapter, currentSection);
                    currentChapter = StripHeadingPrefix(line);
                    currentSection = null;
                }
                else if (level == HeadingLevel.Section)
                {
                    FlushSegment(segments, currentLines, currentPageNumber, currentChapter, currentSection);
                    currentSection = StripHeadingPrefix(line);
                }
                else
                {
                    currentLines.Add(line);
                    currentPageNumber = page.PageNumber;
                }
            }
        }

        FlushSegment(segments, currentLines, currentPageNumber, currentChapter, currentSection);
        return segments;
    }

    private static void FlushSegment(
        List<TextSegment> segments,
        List<string> lines,
        int pageNumber,
        string? chapter,
        string? section)
    {
        if (lines.Count == 0) return;
        var text = string.Join(" ", lines);
        if (!string.IsNullOrWhiteSpace(text))
            segments.Add(new TextSegment(pageNumber, chapter, section, text));
        lines.Clear();
    }

    private sealed record SegmentChunk(
        int PageNumber,
        string? ChapterTitle,
        string? SectionTitle,
        string Text,
        int StartWord,
        int EndWord);

    private static List<SegmentChunk> SplitSegmentIntoChunks(TextSegment segment)
    {
        var words = segment.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<SegmentChunk>();
        int step = ChunkSize - ChunkOverlap;
        int start = 0;

        while (start < words.Length)
        {
            int end = Math.Min(start + ChunkSize, words.Length);
            string chunkText = string.Join(' ', words[start..end]);
            chunks.Add(new SegmentChunk(
                segment.PageNumber,
                segment.ChapterTitle,
                segment.SectionTitle,
                chunkText,
                start,
                end - 1));
            start += step;
        }

        return chunks;
    }
}
