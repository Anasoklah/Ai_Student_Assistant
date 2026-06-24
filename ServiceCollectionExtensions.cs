using System.Text;
using Authentication.interfaces;
using Authentication.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SyrianStudyBot.Data.BackgroundJobs;
using SyrianStudyBot.Domain;
using SyrianStudyBot.interfaces;
using SyrianStudyBot.interfaces.Auth;
using SyrianStudyBot.Services;

namespace SyrianStudyBot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();
        services.AddSingleton<IPdfVisionExtractorService, PdfVisionExtractorService>();
        services.AddSingleton<IPdfTextExtractorService, PdfTextExtractorService>();
        services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
        services.AddScoped<IVectorSearchService, VectorSearchService>();
        services.AddScoped<IRagPipelineService, RagPipelineService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
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

}
