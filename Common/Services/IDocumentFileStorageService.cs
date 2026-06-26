using Microsoft.AspNetCore.Http;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Common.Services;

public interface IDocumentFileStorageService
{
    Task<StoredDocumentFile> SaveAsync(
        IFormFile file,
        DocumentType documentType,
        Guid? userId,
        CancellationToken cancellationToken = default);

    void DeleteIfExists(string? filePath);
}
