using System.ClientModel;
using OpenAI;
using OpenAI.Chat;
using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Services;

public class OpenRouterChatService : IChatService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenRouterChatService> _logger;

    public OpenRouterChatService(IConfiguration configuration, ILogger<OpenRouterChatService> logger)
    {
        _logger = logger;

        var apiKey = configuration["OpenRouter:ApiKey"]!;
        var model  = configuration["OpenRouter:Model"] ?? "qwen/qwen-2.5-72b-instruct";

        // OpenRouter uses an OpenAI-compatible endpoint
        var client = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri("https://openrouter.ai/api/v1") }
        );

        _chatClient = client.GetChatClient(model);
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Calling OpenRouter (Qwen)...");

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userMessage)
        };

        var options = new ChatCompletionOptions { Temperature = 0.2f };

        var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
        return response.Value.Content[0].Text;
    }
}
