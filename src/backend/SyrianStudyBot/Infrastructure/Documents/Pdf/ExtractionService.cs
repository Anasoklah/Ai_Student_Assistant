using SyrianStudyBot.Features.Documents.Dtos;
using SyrianStudyBot.Interfaces;
using SyrianStudyBot.Infrastructure.Ai.Extraction;
using SyrianStudyBot.Infrastructure.Ai.Extraction.Dtos;

namespace SyrianStudyBot.Infrastructure.Documents.Pdf;

/// <summary>
/// Implementation of IPdfTextExtractorService that delegates PDF extraction to the external AI service.
/// This service submits the PDF to the AI service, polls for completion, and returns the extracted pages.
/// </summary>
public class ExtractionService(
    IAiExtractionClient aiExtractionClient,
    ILogger<ExtractionService> logger) : IExtractionService
{
    public async Task<IReadOnlyList<ExtractedPageDto>> ExtractPagesAsync(
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

    public async Task<ImageExtractionResponse> ExtractImageAsync(
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
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI service image extraction failed: {FileName}", fileName);
            throw;
        }
    }
}
