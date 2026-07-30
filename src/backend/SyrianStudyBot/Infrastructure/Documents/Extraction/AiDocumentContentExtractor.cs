using SyrianStudyBot.Application.Documents.Dtos;
using SyrianStudyBot.Infrastructure.Ai.Extraction;
using SyrianStudyBot.Infrastructure.Ai.Extraction.Dtos;
using SyrianStudyBot.Application.Documents;

namespace SyrianStudyBot.Infrastructure.Documents.Extraction;

/// <summary>
/// AI-backed implementation of the document extraction port.
/// It translates provider DTOs into Application DTOs.
/// </summary>
public class AiDocumentContentExtractor(
    IAiExtractionClient aiExtractionClient,
    ILogger<AiDocumentContentExtractor> logger) : IDocumentContentExtractor
{
    public async Task<IReadOnlyList<ExtractedPageDto>> ExtractPdfAsync(
        Stream pdfStream,
        int? startPage,
        int? endPage,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting AI service extraction for PDF (stream)");
        
        var bookId = Guid.NewGuid().ToString();
        
        try
        {
            var jobAccepted = await aiExtractionClient.SubmitExtractionJobAsync(
                pdfStream,
                bookId,
                pageStart: startPage,
                pageEnd: endPage,
                cancellationToken);
            
            logger.LogInformation("Job {JobId} accepted for book {BookId}", jobAccepted.JobId, bookId);
            
            var extractedPages = await aiExtractionClient.ExtractPagesFromJobAsync(
                jobAccepted.JobId,
                cancellationToken);
            
            logger.LogInformation("Successfully extracted {Count} pages from AI service", extractedPages.Count);
            
            return extractedPages;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI service extraction failed for PDF (stream)");
            throw;
        }
    }

    public async Task<ExtractedImageContent> ExtractImageAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting AI service image extraction: {FileName}", fileName);

        try
        {
            var result = await aiExtractionClient.ExtractImageAsync(imageStream, fileName, cancellationToken);
            logger.LogInformation("Image extraction completed: {FileName}, Success: {Success}, Concepts: {Count}",
                fileName, result.Success, result.Concepts?.Count ?? 0);
            return new ExtractedImageContent(
                result.PageNumber,
                (result.Concepts ?? []).Select(concept => new ExtractedConceptDto
                {
                    Title = concept.Title,
                    Content = concept.Content,
                    Keywords = concept.Keywords
                }).ToList(),
                result.NeedsReview);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI service image extraction failed: {FileName}", fileName);
            throw;
        }
    }

    public async Task<BookStructureDto?> ExtractBookStructureAsync(
        Stream pdfStream,
        int tocPage,
        int? tocPageEnd,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting AI service structure extraction (TOC page: {TocPage})", tocPage);

        try
        {
            var result = await aiExtractionClient.ExtractBookStructureAsync(pdfStream, tocPage, tocPageEnd , cancellationToken);

            if (!result.Success || result.Structure is null)
            {
                logger.LogWarning("Structure extraction failed: {Error}", result.ErrorMessage);
                return null;
            }

            logger.LogInformation("Structure extraction completed: {Entries} entries (method: {Method})",
                result.Structure.TotalEntries, result.Structure.ExtractionMethod);
            return new BookStructureDto
            {
                Chapters = result.Structure.Chapters.Select(entry => new BookStructureEntryDto
                {
                    Title = entry.Title,
                    PageNumber = entry.PageNumber,
                    Level = entry.Level,
                    ParentChapter = entry.ParentChapter
                }).ToList(),
                Sections = result.Structure.Sections.Select(entry => new BookStructureEntryDto
                {
                    Title = entry.Title,
                    PageNumber = entry.PageNumber,
                    Level = entry.Level,
                    ParentChapter = entry.ParentChapter
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI service structure extraction failed");
            return null;
        }
    }
}
