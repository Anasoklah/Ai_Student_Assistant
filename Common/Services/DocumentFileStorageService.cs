using Microsoft.Extensions.Options;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Common.Services;

public class DocumentFileStorageService : IDocumentFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly DocumentUploadOptions _options;

    public DocumentFileStorageService(
        IWebHostEnvironment environment,
        IOptions<DocumentUploadOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public async Task<StoredDocumentFile> SaveAsync(
        IFormFile file,
        DocumentType documentType,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var rootPath = GetStorageRootPath();
        var ownerFolder = userId?.ToString("N") ?? "admin";
        var typeFolder = documentType.ToString().ToLowerInvariant();
        var folderPath = Path.Combine(rootPath, typeFolder, ownerFolder);
        Directory.CreateDirectory(folderPath);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(folderPath, fileName);

        await using var output = File.Create(filePath);
        await file.CopyToAsync(output, cancellationToken);

        return new StoredDocumentFile(filePath, file.Length);
    }

    public void DeleteIfExists(string? filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            File.Delete(filePath);
    }

    private string GetStorageRootPath()
    {
        return Path.IsPathRooted(_options.StorageRootPath)
            ? _options.StorageRootPath
            : Path.Combine(_environment.ContentRootPath, _options.StorageRootPath);
    }
}
