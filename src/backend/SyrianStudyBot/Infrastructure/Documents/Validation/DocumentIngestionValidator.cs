using SyrianStudyBot.Application.Documents.Dtos;

namespace SyrianStudyBot.Infrastructure.Documents.Validation;

public class DocumentIngestionValidator : IDocumentIngestionValidator
{
    private static readonly HashSet<string> AllowedFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".txt",
        ".md",
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    };

    public string? ValidateIngestionRequest(DocumentIngestionCommand request)
    {
        if (request is null)
            return "Request body is required.";

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.SourceName))
            return "Title and SourceName are required.";

        if (request.Pages.Count == 0 || request.Pages.All(page => string.IsNullOrWhiteSpace(page.Text)))
            return "At least one page with text is required.";

        return null;
    }

    public string? ValidateFileUploadRequest(UploadDocumentRequest request, long maxFileSizeBytes)
    {
        if (request is null)
            return "Request body is required.";

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.SourceName))
            return "Title and SourceName are required.";

        if (request.File is null || request.File.Length == 0)
            return "A non-empty file is required.";

        if (maxFileSizeBytes <= 0)
            return "Your subscription plan does not allow file uploads.";

        if (request.File.Length > maxFileSizeBytes)
            return $"File size exceeds your plan limit of {FormatBytes(maxFileSizeBytes)}.";

        var extension = Path.GetExtension(request.File.FileName);
        if (!AllowedFileExtensions.Contains(extension))
            return "Only PDF, TXT, and MD files are supported.";

        return null;
    }

    private static string FormatBytes(long bytes)
    {
        var megabytes = bytes / 1024d / 1024d;
        return $"{megabytes:0.#} MB";
    }
}
