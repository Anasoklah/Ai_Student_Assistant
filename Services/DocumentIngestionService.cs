using System.Text.RegularExpressions;
using Pgvector;
using SyrianStudyBot.Data;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Dtos;
using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Services;

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

        // Step 2: Split pages into section-aware segments (each segment knows its chapter/section)
        var segments = SplitIntoSegments(requestDto.Pages);
        logger.LogInformation("Detected {Count} text segments across sections", segments.Count);

        // Step 3: Chunk each segment individually (chunks never cross section boundaries)
        var chunks = segments.SelectMany(SplitSegmentIntoChunks).ToList();
        logger.LogInformation("Split into {Count} chunks", chunks.Count);

        // Step 4: Embed all chunks in one batch call
        var embeddings = await embeddingService.GenerateEmbeddingsAsync(
            chunks.Select(c => c.Text).ToList(), cancellationToken);
        logger.LogInformation("Generated {Count} embeddings", embeddings.Count);

        // Step 5: Build DocumentChunk entities with full section metadata
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

    // ── Section detection ──────────────────────────────────────────────────────

    private enum HeadingLevel { None, Chapter, Section }

    // Detects Arabic (and Markdown-prefixed) headings.
    // Vision extraction produces "# " / "## " prefixes; PdfPig extraction uses Arabic keyword patterns.
    private static HeadingLevel DetectHeadingLevel(string line)
    {
        // Markdown headings produced by vision LLM
        if (line.StartsWith("## ")) return HeadingLevel.Section;
        if (line.StartsWith("# ") && !line.StartsWith("## ")) return HeadingLevel.Chapter;

        // Arabic chapter markers (الفصل / الوحدة / الباب / المحور)
        if (Regex.IsMatch(line, @"^(الفصل|الوحدة|الباب|المحور)\s"))
            return HeadingLevel.Chapter;

        // Arabic section/lesson markers (الدرس / درس)
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

    // ── Segmentation ──────────────────────────────────────────────────────────

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
            // Split on newlines (preserved by the updated PdfPig extractor and by vision LLM output)
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

    // ── Chunking ──────────────────────────────────────────────────────────────

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
