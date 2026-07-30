using SyrianStudyBot.Application.Chat;
using SyrianStudyBot.Application.Documents;
using SyrianStudyBot.Application.Rag;

namespace SyrianStudyBot.Infrastructure.Documents.BackgroundJobs;

public class DocumentProcessingWorker : BackgroundService
{
    private readonly InMemoryDocumentProcessingJobQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentProcessingWorker> _logger;

    public DocumentProcessingWorker(
        InMemoryDocumentProcessingJobQueue queue,
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

        await foreach (var request in _queue.ReadAllAsync(stoppingToken))
        {
            _logger.LogInformation("Processing document {Id}: {FileName}", request.DocumentId, request.FileName);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<DocumentBackgroundProcessor>();
                await processor.ProcessAsync(request, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing document {Id}", request.DocumentId);
            }
        }

        _logger.LogInformation("Document processing worker stopped");
    }
}
