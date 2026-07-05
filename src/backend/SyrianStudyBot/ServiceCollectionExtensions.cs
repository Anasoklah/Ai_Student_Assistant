using System.Text;
using System.Text.Json;
using SyrianStudyBot.Interfaces;
using SyrianStudyBot.Features.Auth.Services;
using SyrianStudyBot.Features.Auth.Services.BackgroundJobs;
using SyrianStudyBot.Features.Auth.Services.Options;
using SyrianStudyBot.Infrastructure.Common;
using SyrianStudyBot.Infrastructure.Documents;
using SyrianStudyBot.Infrastructure.Documents.Validation;
using SyrianStudyBot.Infrastructure.Identity;
using SyrianStudyBot.Infrastructure.Ai.VectorSearch;
using SyrianStudyBot.Infrastructure.Ai.Rag;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SyrianStudyBot.Features.Auth.UseCases;
using SyrianStudyBot.Features.Chat.UseCases;
using SyrianStudyBot.Features.Documents.UseCases;
using SyrianStudyBot.Features.Payments.UseCases;
using SyrianStudyBot.Features.Profile.UseCases;
using SyrianStudyBot.Features.Quiz.UseCases;
using SyrianStudyBot.Infrastructure.Persistence;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Infrastructure.Ai.Chat;
using SyrianStudyBot.Infrastructure.Ai.Embeddings;
using SyrianStudyBot.Infrastructure.Documents.Pdf;

namespace SyrianStudyBot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        services.AddScoped<IPagingService, PagingService>();
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<IUsageTrackingService, UsageTrackingService>();
        services.AddScoped<IDocumentIngestionValidator, DocumentIngestionValidator>();
        services.AddScoped<IDocumentRequestService, DocumentRequestService>();
        return services;
    }

    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();
        services.AddSingleton<IPdfVisionExtractorService, PdfVisionExtractorService>();
        services.AddSingleton<IPdfTextExtractorService, PdfTextExtractorService>();
        services.AddScoped<IDocumentFileExtractionService, DocumentFileExtractionService>();
        services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
        services.AddScoped<IVectorSearchService, VectorSearchService>();
        services.AddScoped<IRagPipelineService, RagPipelineService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAuthUseCase, AuthUseCase>();
        services.AddScoped<IChatUseCase, ChatUseCase>();
        services.AddScoped<IDocumentUseCase, DocumentUseCase>();
        services.AddScoped<IProfileUseCase, ProfileUseCase>();
        services.AddScoped<IPaymentUseCase, PaymentUseCase>();
        services.AddScoped<IQuizUseCase, QuizUseCase>();
        services.AddHttpContextAccessor();
        services.AddHostedService<TokenCleanupService>();
        return services;
    }

    public static IServiceCollection AddIdentityService(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student", "Admin"));
        });

        return services;
    }

    public static IServiceCollection AddJwtService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options =>
        {
            options.IncludeErrorDetails = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]
                        ?? throw new InvalidOperationException("Jwt:Secret is not configured."))),
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    if (context.Response.HasStarted)
                        return;

                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    var detail = context.ErrorDescription
                        ?? context.AuthenticateFailure?.Message
                        ?? "Authentication failed.";

                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        message = "Unauthorized",
                        detail
                    }));
                }
            };
        });

        return services;

    }

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

    public static IServiceCollection AddSettingsServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.Configure<DocumentUploadOptions>(configuration.GetSection("DocumentUploads"));
        return services;
    }

    public static IServiceCollection AddAiExtractionClient(this IServiceCollection services, IConfiguration configuration)
    {
        var aiServiceConfig = configuration.GetSection("AiService");
        var baseUrl = aiServiceConfig["BaseUrl"] ?? "http://localhost:8000";
        var pollingIntervalSeconds = int.Parse(aiServiceConfig["PollingIntervalSeconds"] ?? "5");
        var timeoutMinutes = int.Parse(aiServiceConfig["TimeoutMinutes"] ?? "30");
        var enabled = bool.Parse(aiServiceConfig["Enabled"] ?? "false");

        // Register the HttpClient with Polly policies
        services.AddHttpClient("AiExtractionClient", (sp, client) =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMinutes(timeoutMinutes);
        })
        .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(2)))
        .AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

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

        // Conditionally register the extraction service based on configuration
        if (enabled)
        {
            services.AddSingleton<IPdfTextExtractorService, AiServiceExtractionService>();
        }

        return services;
    }

}
