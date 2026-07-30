using SyrianStudyBot.Application.Documents.Dtos;

namespace SyrianStudyBot.Application.Documents;

/// <summary>
/// Extracts educational content from an uploaded document.
/// Infrastructure provides the AI-backed implementation.
/// </summary>
public interface IDocumentContentExtractor
{
    Task<IReadOnlyList<ExtractedPageDto>> ExtractPdfAsync(
        Stream pdfStream,
        int? startPage,
        int? endPage,
        CancellationToken cancellationToken = default);

    Task<ExtractedImageContent> ExtractImageAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<BookStructureDto?> ExtractBookStructureAsync(
        Stream pdfStream,
        int tocPage,
        int? tocPageEnd,
        CancellationToken cancellationToken = default);
}
