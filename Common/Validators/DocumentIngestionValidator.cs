using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.Common.Validators;

public class DocumentIngestionValidator : IDocumentIngestionValidator
{
    public string? ValidateIngestionRequest(DocumentIngestionRequestDto request)
    {
        if (request is null)
            return "Request body is required.";

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.SourceName))
            return "Title and SourceName are required.";

        if (request.Pages.Count == 0 || request.Pages.All(page => string.IsNullOrWhiteSpace(page.Text)))
            return "At least one page with text is required.";

        return null;
    }
}
