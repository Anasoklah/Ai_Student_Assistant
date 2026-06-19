using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SyrianStudyBot.Domain;
using SyrianStudyBot.interfaces;
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
                Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
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
