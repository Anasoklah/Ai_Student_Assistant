using SyrianStudyBot.Infrastructure.Documents.BackgroundJobs;

namespace SyrianStudyBot.Application.Documents;

public interface IDocumentProcessingQueue
{
    Task EnqueueAsync(DocumentProcessingJob job, CancellationToken ct = default);
    IAsyncEnumerable<DocumentProcessingJob> DequeueAllAsync(CancellationToken ct = default);
}
