using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.interfaces;

public interface IPdfVisionExtractorService
{
    Task<IReadOnlyList<ExtractedPageDto>> ExtractTextAsync(byte[] pdfBytes, CancellationToken cancellationToken = default);
}
