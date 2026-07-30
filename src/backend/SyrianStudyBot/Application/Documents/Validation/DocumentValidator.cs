using SyrianStudyBot.Application.Documents.Dtos;
using SyrianStudyBot.Application.Documents.Commands;

namespace SyrianStudyBot.Application.Documents.Validation;

public class DocumentValidator : IDocumentValidator
{
    private static readonly HashSet<string> AllowedFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    };

    public string? ValidateExtractedContent(IReadOnlyList<ExtractedPageDto> pages)
    {
        if (pages.Count == 0 || pages.All(page => string.IsNullOrWhiteSpace(page.Text) && page.Concepts.Count == 0))
            return "At least one page with text is required.";

        return null;
    }

    public string? ValidateUpload(UploadDocumentCommand command, long maxFileSizeBytes)
    {
        if (command is null)
            return "Request body is required.";

        if (string.IsNullOrWhiteSpace(command.Title) || string.IsNullOrWhiteSpace(command.SourceName))
            return "Title and SourceName are required.";

        if (command.FileContent == Stream.Null || command.FileSizeBytes == 0)
            return "A non-empty file is required.";

        if (maxFileSizeBytes <= 0)
            return "Your subscription plan does not allow file uploads.";

        if (command.FileSizeBytes > maxFileSizeBytes)
            return $"File size exceeds your plan limit of {FormatBytes(maxFileSizeBytes)}.";

        var extension = Path.GetExtension(command.FileName);
        if (!AllowedFileExtensions.Contains(extension))
            return "Only PDF and image files are supported.";

        return null;
    }

    private static string FormatBytes(long bytes)
    {
        var megabytes = bytes / 1024d / 1024d;
        return $"{megabytes:0.#} MB";
    }
}
