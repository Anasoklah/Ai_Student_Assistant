using SyrianStudyBot.Features.Documents.Dtos;
using SyrianStudyBot.Infrastructure.Ai.Extraction.Dtos;

namespace SyrianStudyBot.Interfaces;

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
        CancellationToken cancellationToken = default);
}
