using System.ClientModel;
using OpenAI;
using OpenAI.Chat;
using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Services;

public class GeminiChatService : IChatService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<GeminiChatService> _logger;

    public GeminiChatService(IConfiguration configuration, ILogger<GeminiChatService> logger)
    {
        _logger = logger;

        var apiKey = configuration["Gemini:ApiKey"]!;
        var model  = configuration["Gemini:Model"] ?? "gemini-2.0-flash";

        // Gemini exposes an OpenAI-compatible endpoint, so we reuse the same OpenAI SDK
        var client = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/") }
        );

        _chatClient = client.GetChatClient(model);
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Calling Gemini...");

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
