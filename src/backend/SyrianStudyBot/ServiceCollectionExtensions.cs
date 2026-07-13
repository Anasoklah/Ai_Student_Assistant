using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
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
using SyrianStudyBot.Infrastructure.Persistence.Repositories;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Domain.Enums;
using SyrianStudyBot.Infrastructure.Ai.Chat;
using SyrianStudyBot.Infrastructure.Ai.Embeddings;
using SyrianStudyBot.Infrastructure.Documents.Pdf;
using SyrianStudyBot.Infrastructure.Ai.Extraction;
using SyrianStudyBot.Features.contracts.repositories;
using SyrianStudyBot.Features.contracts.services;
using SyrianStudyBot.Infrastructure.Documents.BackgroundJobs;

namespace SyrianStudyBot;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers infrastructure-level services that don't belong to a specific feature.
    /// User context, usage tracking, and document validation are cross-cutting concerns.
    /// </summary>
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<IUsageTrackingService, UsageTrackingService>();
        services.AddScoped<IDocumentIngestionValidator, DocumentIngestionValidator>();
        return services;
    }

    /// <summary>
    /// Registers all repositories and core application services.
    /// Repositories encapsulate all AppDbContext access — UseCases and Services
    /// should never inject AppDbContext directly.
    /// </summary>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // ── Repositories (all scoped, sharing the same AppDbContext per request) ──
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IQuizRepository, QuizRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUsageRepository, UsageRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // ── External AI/API services (no database access) ──
        services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();
        services.AddScoped<IRagPipelineService, RagPipelineService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        // ── Application services (business logic, uses repositories) ──
        services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
        services.AddScoped<IVectorSearchService, VectorSearchService>();

        // ── UseCases (orchestration layer) ──
        services.AddScoped<IAuthUseCase, AuthUseCase>();
        services.AddScoped<IChatUseCase, ChatUseCase>();
        services.AddScoped<IDocumentUseCase, DocumentUseCase>();
        services.AddScoped<IProfileUseCase, ProfileUseCase>();
        services.AddScoped<IPaymentUseCase, PaymentUseCase>();
        services.AddScoped<IQuizUseCase, QuizUseCase>();
        services.AddHttpContextAccessor();

        // Background jobs
        services.AddHostedService<TokenCleanupService>();
        services.AddSingleton<IDocumentProcessingQueue, DocumentProcessingQueue>();
        services.AddScoped<IDocumentProcessor, DocumentProcessor>();
        services.AddHostedService<DocumentProcessingWorker>();
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

    public static async Task SeedRoles( IServiceProvider provider)
    {
         using var scope =  provider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach(var role in new[] {"Admin" , "Student"})
        {
              if (!await roleManager.RoleExistsAsync(role))
               await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }

    public static async Task SeedAdminUser (IServiceProvider provider)
    {
         using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        


        var adminEmail = config["Admin:Email"];
        var adminPassword = config["Admin:Password"];

        logger.LogInformation("Seeding admin user: {Email}", adminEmail);

        if(!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if(adminUser is null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "AdminUser",
                    Email = adminEmail,
                    EmailConfirmed = true,
                    TwoFactorEnabled = false,
                    PhoneNumber = "+963983050315"  
                };
                var result = await userManager.CreateAsync(adminUser , adminPassword);
                if (result.Succeeded)
                {
                 logger.LogInformation("Admin user created successfully.");
                 await userManager.AddToRoleAsync(adminUser , "Admin");
                }
                else
                {
                    logger.LogError("Failed to create admin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }            
        }
    }

    /// <summary>
    /// On startup, marks any documents stuck in <see cref="DocumentStatus.Processing"/>
    /// as <see cref="DocumentStatus.Failed"/> because the in-memory job queue was lost
    /// on restart. Documents uploaded close to a restart boundary can be re-uploaded.
    /// </summary>
    public static async Task ReconcileStaleDocuments(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var staleDocs = await db.Documents
            .Where(d => d.Status == DocumentStatus.Processing)
            .ToListAsync();

        foreach (var doc in staleDocs)
        {
            doc.Status = DocumentStatus.Failed;
            doc.StatusMessage = "Processing interrupted by server restart. Please re-upload.";
            doc.ProcessedAt = DateTime.UtcNow;
        }

        if (staleDocs.Count > 0)
        {
            await db.SaveChangesAsync();
            logger.LogWarning("Marked {Count} stale processing documents as failed", staleDocs.Count);
        }
    }
}
