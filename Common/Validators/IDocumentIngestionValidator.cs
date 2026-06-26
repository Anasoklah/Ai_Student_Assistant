using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.Common.Validators;

public interface IDocumentIngestionValidator
{
    string? ValidateIngestionRequest(DocumentIngestionRequestDto request);
    string? ValidateFileUploadRequest(DocumentFileUploadRequestDto request, long maxFileSizeBytes);
}
