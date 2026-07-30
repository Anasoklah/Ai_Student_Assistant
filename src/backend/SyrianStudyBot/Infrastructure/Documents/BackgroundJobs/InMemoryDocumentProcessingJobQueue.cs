using System.Threading.Channels;
using SyrianStudyBot.Application.Chat;
using SyrianStudyBot.Application.Documents;
using SyrianStudyBot.Application.Rag;

namespace SyrianStudyBot.Infrastructure.Documents.BackgroundJobs;

/// <summary>
/// In-memory implementation of the application queue port.
/// Jobs are lost when the API process restarts; use a durable queue when that
/// guarantee becomes necessary.
/// </summary>
public class InMemoryDocumentProcessingJobQueue : IDocumentProcessingJobQueue
{
    private readonly Channel<DocumentProcessingRequest> _channel = Channel.CreateBounded<DocumentProcessingRequest>(
        new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        });

    public async Task EnqueueAsync(DocumentProcessingRequest request, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(request, cancellationToken);
    }

    public IAsyncEnumerable<DocumentProcessingRequest> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
