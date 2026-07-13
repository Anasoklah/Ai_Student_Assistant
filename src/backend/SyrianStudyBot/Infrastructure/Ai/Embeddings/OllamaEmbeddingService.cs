using OllamaSharp;
using OllamaSharp.Models;
using SyrianStudyBot.Application.Chat;
using SyrianStudyBot.Application.Documents;
using SyrianStudyBot.Application.Rag;

namespace SyrianStudyBot.Infrastructure.Ai.Embeddings;

public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly OllamaApiClient _client;
    private readonly string _model;
    private readonly ILogger<OllamaEmbeddingService> _logger;

    public OllamaEmbeddingService(IConfiguration configuration, ILogger<OllamaEmbeddingService> logger)
    {
        _logger = logger;
        var baseUrl = configuration["OLLAMA_HOST"] ?? "http://localhost:11434";
        _model = configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";

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
            const int batchSize = 10;
            const int maxConcurrency = 3;

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
