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
        byte[] pdfBytes,
        int? startPage,
        int? EndPage,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting AI service extraction for PDF ({Size} bytes)", pdfBytes.Length);
        
        // Generate a book_id for this extraction session
        // In a real scenario, this would come from the database
        var bookId = Guid.NewGuid().ToString();
        
        try
        {
            // Submit the extraction job to the AI service
            var jobAccepted = await aiExtractionClient.SubmitExtractionJobAsync(
                pdfBytes,
                bookId,
                pageStart: startPage,
                pageEnd: EndPage,
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
            logger.LogError(ex, "AI service extraction failed for PDF ({Size} bytes)", pdfBytes.Length);
            throw;
        }
    }

  
}
