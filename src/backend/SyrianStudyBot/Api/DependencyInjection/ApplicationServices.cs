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

public static class ApplicationServices
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

    public static IServiceCollection AddSettingsServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.Configure<DocumentUploadOptions>(configuration.GetSection("DocumentUploads"));
        return services;
    }
}
