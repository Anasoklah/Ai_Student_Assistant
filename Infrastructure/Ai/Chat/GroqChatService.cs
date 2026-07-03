using System.ClientModel;
using OpenAI;
using OpenAI.Chat;
using SyrianStudyBot.Interfaces;

namespace SyrianStudyBot.Infrastructure.Ai.Chat;

public class GroqChatService : IChatService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<GroqChatService> _logger;

    public GroqChatService(IConfiguration configuration, ILogger<GroqChatService> logger)
    {
        _logger = logger;

        var apiKey = configuration["Groq:ApiKey"]!;
        var model = configuration["Groq:Model"] ?? "llama-3.3-70b-versatile";

        var client = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") }
        );

        _chatClient = client.GetChatClient(model);
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Calling Groq LLM...");

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
