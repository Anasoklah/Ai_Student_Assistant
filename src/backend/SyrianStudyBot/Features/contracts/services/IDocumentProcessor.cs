using SyrianStudyBot.Infrastructure.Documents.BackgroundJobs;

namespace SyrianStudyBot.Features.contracts.services;

public interface IDocumentProcessor
{
    Task ProcessAsync(DocumentProcessingJob job, CancellationToken ct = default);
}
