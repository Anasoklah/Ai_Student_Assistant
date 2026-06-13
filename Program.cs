using Microsoft.EntityFrameworkCore;
using SyrianStudyBot;
using SyrianStudyBot.Data;
using SyrianStudyBot.interfaces;
using SyrianStudyBot.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"),
        o => o.UseVector());
});

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Services
builder.Services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();
builder.Services.AddSingleton<IPdfVisionExtractorService, PdfVisionExtractorService>();
builder.Services.AddSingleton<IPdfTextExtractorService, PdfTextExtractorService>();
builder.Services.AddSingleton<ITelegramUpdateHandler, TelegramUpdateHandler>();

builder.Services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
builder.Services.AddScoped<IVectorSearchService, VectorSearchService>();
builder.Services.AddScoped<IUserSessionService, UserSessionService>();
builder.Services.AddScoped<ITelegramCommandHandler, TelegramCommandHandler>();
builder.Services.AddScoped<ITelegramDocumentUploadHandler, TelegramDocumentUploadHandler>();
builder.Services.AddScoped<IRagPipelineService, RagPipelineService>();

// Chat Provider
var chatProvider = builder.Configuration["ChatProvider"] ?? "Groq";

builder.Services.AddSingleton<IChatService>(_ =>
{
    var loggerFactory = LoggerFactory.Create(b => b.AddConsole());

    return chatProvider switch
    {
        "Gemini" => new GeminiChatService(
            builder.Configuration,
            loggerFactory.CreateLogger<GeminiChatService>()),

        "DeepSeek" => new DeepSeekChatService(
            builder.Configuration,
            loggerFactory.CreateLogger<DeepSeekChatService>()),

        "OpenRouter" => new OpenRouterChatService(
            builder.Configuration,
            loggerFactory.CreateLogger<OpenRouterChatService>()),

        _ => new GroqChatService(
            builder.Configuration,
            loggerFactory.CreateLogger<GroqChatService>())
    };
});

// Background Worker
builder.Services.AddHostedService<Worker>();

var app = builder.Build();

// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();