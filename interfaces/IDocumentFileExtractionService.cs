using Microsoft.AspNetCore.Http;
using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.interfaces;

public interface IDocumentFileExtractionService
{
    Task<IReadOnlyList<ExtractedPageDto>> ExtractPagesAsync(
        IFormFile file,
        bool forceVision,
        CancellationToken cancellationToken = default);
}
