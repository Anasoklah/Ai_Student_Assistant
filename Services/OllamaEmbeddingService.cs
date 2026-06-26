using OllamaSharp;
using OllamaSharp.Models;
using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Services;

public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly OllamaApiClient _client;
    private readonly string _model;
    private readonly ILogger<OllamaEmbeddingService> _logger;

    public OllamaEmbeddingService(IConfiguration configuration, ILogger<OllamaEmbeddingService> logger)
    {
        _logger = logger;
        var baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _model = configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";

        // Embedding many chunks on CPU can take several minutes — use a long timeout
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromHours(1)
        };
        _client = new OllamaApiClient(httpClient);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating embedding for text of length {Length}", text.Length);

        var request = new EmbedRequest { Model = _model, Input = [text] };
        var response = await _client.EmbedAsync(request, cancellationToken);

        return response.Embeddings[0].ToArray();
    }

        public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
            IEnumerable<string> texts, CancellationToken cancellationToken = default)
        {
            var inputList = texts.ToList();
            const int batchSize = 10; // tune based on your hardware
            const int maxConcurrency = 3; // don't overwhelm Ollama

            var results = new float[inputList.Count][];
            var semaphore = new SemaphoreSlim(maxConcurrency);

            var tasks = inputList.Select(async (text, index) =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var request = new EmbedRequest { Model = _model, Input = [text] };
                    var response = await _client.EmbedAsync(request, cancellationToken);
                    results[index] = response.Embeddings[0].ToArray();
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            return results;
        }
}
