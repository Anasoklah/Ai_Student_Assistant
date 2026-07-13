using SyrianStudyBot.Infrastructure.Documents.BackgroundJobs;

namespace SyrianStudyBot.Application.Documents;

public interface IDocumentProcessor
{
    Task ProcessAsync(DocumentProcessingJob job, CancellationToken ct = default);
}
