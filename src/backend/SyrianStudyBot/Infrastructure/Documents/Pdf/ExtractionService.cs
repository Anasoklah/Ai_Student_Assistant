using SyrianStudyBot.Features.Documents.Dtos;
using SyrianStudyBot.Interfaces;
using SyrianStudyBot.Infrastructure.Ai.Extraction;

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
        
        // Generate a book_id for this extraction session
        var bookId = Guid.NewGuid().ToString();
        
        try
        {
            // Submit the extraction job to the AI service (streaming, no byte[] buffer)
            var jobAccepted = await aiExtractionClient.SubmitExtractionJobAsync(
                pdfStream,
                bookId,
                pageStart: startPage,
                pageEnd: endPage,
                cancellationToken);
            
            logger.LogInformation("Job {JobId} accepted for book {BookId}", jobAccepted.JobId, bookId);
            
            // Poll for completion and get the results
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

  
}
