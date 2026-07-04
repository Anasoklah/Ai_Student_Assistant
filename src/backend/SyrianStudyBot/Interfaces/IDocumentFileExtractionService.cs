using SyrianStudyBot.Features.Documents.Dtos;

namespace SyrianStudyBot.Interfaces;

public interface IDocumentFileExtractionService
{
    Task<IReadOnlyList<ExtractedPageDto>> ExtractPagesAsync(
        Microsoft.AspNetCore.Http.IFormFile file,
        bool forceVision,
        CancellationToken cancellationToken = default);
}
