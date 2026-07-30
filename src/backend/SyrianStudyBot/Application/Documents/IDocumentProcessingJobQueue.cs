namespace SyrianStudyBot.Application.Documents;

/// <summary>
/// Schedules background processing after a document upload is persisted.
/// </summary>
public interface IDocumentProcessingJobQueue
{
    Task EnqueueAsync(DocumentProcessingRequest request, CancellationToken cancellationToken = default);
}
