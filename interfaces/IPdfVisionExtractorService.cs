namespace SyrianStudyBot.interfaces;

public interface IPdfVisionExtractorService
{
    Task<string> ExtractTextAsync(byte[] pdfBytes, CancellationToken cancellationToken = default);
}
