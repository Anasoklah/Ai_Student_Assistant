using Microsoft.EntityFrameworkCore;
using SyrianStudyBot;
using SyrianStudyBot.Data;
using SyrianStudyBot.interfaces;
using SyrianStudyBot.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"),
        o => o.UseVector()
    )
);

builder.Services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();
builder.Services.AddSingleton<IPdfVisionExtractorService, PdfVisionExtractorService>();
builder.Services.AddSingleton<IPdfTextExtractorService, PdfTextExtractorService>();
builder.Services.AddSingleton<ITelegramUpdateHandler, TelegramUpdateHandler>();
builder.Services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
builder.Services.AddScoped<IVectorSearchService, VectorSearchService>();
builder.Services.AddScoped<IUserSessionService, UserSessionService>();
builder.Services.AddScoped<ITelegramCommandHandler, TelegramCommandHandler>();
builder.Services.AddScoped<ITelegramDocumentUploadHandler, TelegramDocumentUploadHandler>();
// Switch providers via "ChatProvider" in appsettings.json: "Groq" | "Gemini" | "DeepSeek" | "OpenRouter"
var chatProvider = builder.Configuration["ChatProvider"] ?? "Groq";
builder.Services.AddSingleton<IChatService>(chatProvider switch
{
    "Gemini"     => new GeminiChatService(builder.Configuration,     LoggerFactory.Create(b => b.AddConsole()).CreateLogger<GeminiChatService>()),
    "DeepSeek"   => new DeepSeekChatService(builder.Configuration,   LoggerFactory.Create(b => b.AddConsole()).CreateLogger<DeepSeekChatService>()),
    "OpenRouter" => new OpenRouterChatService(builder.Configuration, LoggerFactory.Create(b => b.AddConsole()).CreateLogger<OpenRouterChatService>()),
    _            => new GroqChatService(builder.Configuration,       LoggerFactory.Create(b => b.AddConsole()).CreateLogger<GroqChatService>())
});
builder.Services.AddScoped<IRagPipelineService, RagPipelineService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
