using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using SyrianStudyBot.Application.Auth;
using SyrianStudyBot.Application.Auth.Options;
using SyrianStudyBot.Application.Chat;
using SyrianStudyBot.Application.Common;
using SyrianStudyBot.Application.Documents;
using SyrianStudyBot.Application.Payments;
using SyrianStudyBot.Application.Profile;
using SyrianStudyBot.Application.Quiz;
using SyrianStudyBot.Application.Rag;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Infrastructure.Ai.Chat;
using SyrianStudyBot.Infrastructure.Ai.Embeddings;
using SyrianStudyBot.Infrastructure.Ai.Extraction;
using SyrianStudyBot.Infrastructure.Ai.Rag;
using SyrianStudyBot.Infrastructure.Ai.VectorSearch;
using SyrianStudyBot.Infrastructure.Auth;
using SyrianStudyBot.Infrastructure.Auth.BackgroundJobs;
using SyrianStudyBot.Infrastructure.Common;
using SyrianStudyBot.Infrastructure.Documents;
using SyrianStudyBot.Infrastructure.Documents.BackgroundJobs;
using SyrianStudyBot.Infrastructure.Documents.Pdf;
using SyrianStudyBot.Infrastructure.Documents.Validation;
using SyrianStudyBot.Infrastructure.Identity;
using SyrianStudyBot.Infrastructure.Persistence;
using SyrianStudyBot.Infrastructure.Persistence.Repositories;
using System.Text;
using System.Text.Json;

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

        // Use Python AI service as the only PDF extraction engine.
        services.AddSingleton<IExtractionService, ExtractionService>();

        return services;
    }
}
