using SyrianStudyBot.Features.Documents.Dtos;

namespace SyrianStudyBot.Interfaces;

public interface IExtractionService
{
    Task<IReadOnlyList<ExtractedPageDto>> ExtractPagesAsync(
        Stream pdfStream,
        int? startPage,
        int? endPage,
        CancellationToken cancellationToken = default);
}
