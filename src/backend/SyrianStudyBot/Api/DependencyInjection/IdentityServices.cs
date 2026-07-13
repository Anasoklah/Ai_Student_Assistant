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

public static class IdentityServices
{
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
}
