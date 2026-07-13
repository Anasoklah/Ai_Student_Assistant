using SyrianStudyBot.Application.Documents.Dtos;
using SyrianStudyBot.Infrastructure.Ai.Extraction.Dtos;

namespace SyrianStudyBot.Application.Documents;

public interface IExtractionService
{
    Task<IReadOnlyList<ExtractedPageDto>> ExtractPagesAsync(
        Stream pdfStream,
        int? startPage,
        int? endPage,
        CancellationToken cancellationToken = default);

    Task<ImageExtractionResponse> ExtractImageAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<DocumentStructureResult?> ExtractStructureAsync(
        Stream pdfStream,
        int tocPage,
        int? tocPageEnd,
        CancellationToken cancellationToken = default);
}
