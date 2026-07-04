using SyrianStudyBot.Features.Documents.Dtos;

namespace SyrianStudyBot.Infrastructure.Documents.Validation;

public interface IDocumentIngestionValidator
{
    string? ValidateIngestionRequest(DocumentIngestionRequestDto request);
    string? ValidateFileUploadRequest(DocumentFileUploadRequestDto request, long maxFileSizeBytes);
}
