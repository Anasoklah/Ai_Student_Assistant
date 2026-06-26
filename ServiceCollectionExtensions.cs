using System.Text;
using Authentication.interfaces;
using Authentication.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SyrianStudyBot.Application.UseCases;
using SyrianStudyBot.Common.Services;
using SyrianStudyBot.Common.Validators;
using SyrianStudyBot.Data.BackgroundJobs;
using SyrianStudyBot.Domain;
using SyrianStudyBot.interfaces;
using SyrianStudyBot.interfaces.Auth;
using SyrianStudyBot.Services;

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
        // ── Identity ──
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

    public static IServiceCollection AddJwtService(this IServiceCollection services , IConfiguration configuration)
    {
        // ── JWT Authentication ──
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
        {
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

    public static IServiceCollection AddSettingsServices(this IServiceCollection services , IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.Configure<DocumentUploadOptions>(configuration.GetSection("DocumentUploads"));
        return services;
    }

}
