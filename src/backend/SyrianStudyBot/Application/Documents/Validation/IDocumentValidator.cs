using SyrianStudyBot.Application.Documents.Dtos;
using SyrianStudyBot.Application.Documents.Commands;

namespace SyrianStudyBot.Application.Documents.Validation;

/// <summary>
/// Validates an upload before it is queued and extracted content before it is saved.
/// </summary>
public interface IDocumentValidator
{
    string? ValidateExtractedContent(IReadOnlyList<ExtractedPageDto> pages);
    string? ValidateUpload(UploadDocumentCommand command, long maxFileSizeBytes);
}
