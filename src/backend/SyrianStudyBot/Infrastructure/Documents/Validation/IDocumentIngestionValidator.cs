using SyrianStudyBot.Application.Documents.Dtos;

namespace SyrianStudyBot.Infrastructure.Documents.Validation;

public interface IDocumentIngestionValidator
{
    string? ValidateIngestionRequest(DocumentIngestionCommand request);
    string? ValidateFileUploadRequest(UploadDocumentRequest request, long maxFileSizeBytes);
}
