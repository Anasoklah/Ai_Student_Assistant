using SyrianStudyBot.Features.Documents.Dtos;

namespace SyrianStudyBot.Interfaces;

public interface IPdfVisionExtractorService
{
    Task<IReadOnlyList<ExtractedPageDto>> ExtractTextAsync(byte[] pdfBytes, CancellationToken cancellationToken = default);
}
