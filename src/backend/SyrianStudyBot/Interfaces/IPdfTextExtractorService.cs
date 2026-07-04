using SyrianStudyBot.Features.Documents.Dtos;

namespace SyrianStudyBot.Interfaces;

public interface IPdfTextExtractorService
{
    Task<IReadOnlyList<ExtractedPageDto>> ExtractPagesAsync(
        byte[] pdfBytes,
        bool forceVision,
        Func<Task> beforeVisionExtraction,
        CancellationToken cancellationToken = default);
}
