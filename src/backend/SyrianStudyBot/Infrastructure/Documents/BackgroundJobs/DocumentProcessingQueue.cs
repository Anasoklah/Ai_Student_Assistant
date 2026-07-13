using System.Threading.Channels;
using SyrianStudyBot.Application.Chat;
using SyrianStudyBot.Application.Documents;
using SyrianStudyBot.Application.Rag;

namespace SyrianStudyBot.Infrastructure.Documents.BackgroundJobs;

public class DocumentProcessingQueue : IDocumentProcessingQueue
{
    private readonly Channel<DocumentProcessingJob> _channel = Channel.CreateBounded<DocumentProcessingJob>(
        new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        });

    public async Task EnqueueAsync(DocumentProcessingJob job, CancellationToken ct = default)
    {
        await _channel.Writer.WriteAsync(job, ct);
    }

    public IAsyncEnumerable<DocumentProcessingJob> DequeueAllAsync(CancellationToken ct = default)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }
}
