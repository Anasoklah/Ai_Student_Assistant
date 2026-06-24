using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.Common.Validators;

public interface IDocumentIngestionValidator
{
    string? ValidateIngestionRequest(DocumentIngestionRequestDto request);
}
