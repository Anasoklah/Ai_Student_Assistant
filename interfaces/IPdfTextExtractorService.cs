using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.interfaces;

public interface IPdfTextExtractorService
{
    Task<IReadOnlyList<ExtractedPageDto>> ExtractPagesAsync(
        byte[] pdfBytes,
        bool forceVision,
        Func<Task> beforeVisionExtraction,
        CancellationToken cancellationToken = default);
}
