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

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var inputList = texts.ToList();
        _logger.LogDebug("Generating embeddings for {Count} texts", inputList.Count);

        // Embed one chunk at a time — sending all chunks in one request exceeds
        // the model's context window when the document is large
        List<float[]> results = [];
        for (int i = 0; i < inputList.Count; i++)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Embedding chunk {Current}/{Total}", i + 1, inputList.Count);
            var request = new EmbedRequest { Model = _model, Input = [inputList[i]] };
            var response = await _client.EmbedAsync(request, cancellationToken);
            results.Add(response.Embeddings[0].ToArray());
        }

        return results;
    }
}
