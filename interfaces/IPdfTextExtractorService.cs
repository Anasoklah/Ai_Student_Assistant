namespace SyrianStudyBot.interfaces;

public interface IPdfTextExtractorService
{
    Task<string> ExtractTextAsync(
        byte[] pdfBytes,
        bool forceVision,
        Func<Task> beforeVisionExtraction,
        CancellationToken cancellationToken = default);
}
