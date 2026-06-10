using SyrianStudyBot.Domain;

namespace SyrianStudyBot.interfaces;

public interface IDocumentIngestionService
{
    // Takes the raw text of a study document, splits it into chunks,
    // embeds each chunk, and saves everything to the database.
    Task<Document> IngestAsync(string title, string subject, string rawText, CancellationToken cancellationToken = default);
}
