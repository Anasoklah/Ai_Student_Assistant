using SyrianStudyBot.Features.contracts.services;

namespace SyrianStudyBot.Infrastructure.Documents.BackgroundJobs;

public class DocumentProcessingWorker : BackgroundService
{
    private readonly IDocumentProcessingQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentProcessingWorker> _logger;

    public DocumentProcessingWorker(
        IDocumentProcessingQueue queue,
        IServiceProvider serviceProvider,
        ILogger<DocumentProcessingWorker> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Document processing worker started");

        await foreach (var job in _queue.DequeueAllAsync(stoppingToken))
        {
            _logger.LogInformation("Processing document {Id}: {FileName}", job.DocumentId, job.FileName);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IDocumentProcessor>();
                await processor.ProcessAsync(job, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing document {Id}", job.DocumentId);
            }
        }

        _logger.LogInformation("Document processing worker stopped");
    }
}
