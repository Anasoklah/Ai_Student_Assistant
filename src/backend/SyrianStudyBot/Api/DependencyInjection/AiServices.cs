using Microsoft.AspNetCore.Authentication.JwtBearer;
using Polly;
using Polly.Extensions.Http;
using SyrianStudyBot.Application.Chat;
using SyrianStudyBot.Application.Documents;
using SyrianStudyBot.Infrastructure.Ai.Chat;
using SyrianStudyBot.Infrastructure.Ai.Extraction;
using SyrianStudyBot.Infrastructure.Documents.Extraction;

namespace SyrianStudyBot;

public static class AiServices
{
    public static IServiceCollection AddChatService(this IServiceCollection services)
    {
        services.AddSingleton<IChatService>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var chatProvider = configuration["ChatProvider"] ?? "Groq";

            return chatProvider switch
            {
                "OpenRouter" => new OpenRouterChatService(
                    configuration,
                    loggerFactory.CreateLogger<OpenRouterChatService>()),

                _ => new GroqChatService(
                    configuration,
                    loggerFactory.CreateLogger<GroqChatService>())
            };
        });

        return services;
    }

    public static IServiceCollection AddAiExtractionClient(this IServiceCollection services, IConfiguration configuration)
    {
        var aiServiceConfig = configuration.GetSection("AiService");
        var baseUrl = aiServiceConfig["BaseUrl"] ?? "http://localhost:8000";
        var pollingIntervalSeconds = int.Parse(aiServiceConfig["PollingIntervalSeconds"] ?? "5");
        var timeoutMinutes = int.Parse(aiServiceConfig["TimeoutMinutes"] ?? "30");

        // Register the HttpClient with Polly policies
        services.AddHttpClient("AiExtractionClient", (sp, client) =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMinutes(timeoutMinutes);
        })
        .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError()
            .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(2)))
        .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

        // Register the client wrapper
        services.AddSingleton<IAiExtractionClient>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<AiExtractionClient>>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            return new AiExtractionClient(
                httpClientFactory.CreateClient("AiExtractionClient"),
                logger,
                TimeSpan.FromSeconds(pollingIntervalSeconds),
                TimeSpan.FromMinutes(timeoutMinutes)
            );
        });

        services.AddSingleton<IDocumentContentExtractor, AiDocumentContentExtractor>();

        return services;
    }
}
