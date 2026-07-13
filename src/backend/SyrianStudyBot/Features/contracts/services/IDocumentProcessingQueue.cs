using SyrianStudyBot.Infrastructure.Documents.BackgroundJobs;

namespace SyrianStudyBot.Features.contracts.services;

public interface IDocumentProcessingQueue
{
    Task EnqueueAsync(DocumentProcessingJob job, CancellationToken ct = default);
    IAsyncEnumerable<DocumentProcessingJob> DequeueAllAsync(CancellationToken ct = default);
}
