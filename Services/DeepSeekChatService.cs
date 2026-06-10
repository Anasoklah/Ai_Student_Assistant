using System.ClientModel;
using OpenAI;
using OpenAI.Chat;
using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Services;

public class DeepSeekChatService : IChatService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<DeepSeekChatService> _logger;

    public DeepSeekChatService(IConfiguration configuration, ILogger<DeepSeekChatService> logger)
    {
        _logger = logger;

        var apiKey = configuration["DeepSeek:ApiKey"]!;
        var model  = configuration["DeepSeek:Model"] ?? "deepseek-chat";

        // DeepSeek uses an OpenAI-compatible endpoint
        var client = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri("https://api.deepseek.com") }
        );

        _chatClient = client.GetChatClient(model);
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Calling DeepSeek...");

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
