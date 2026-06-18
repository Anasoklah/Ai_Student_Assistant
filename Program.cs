using Microsoft.EntityFrameworkCore;
using SyrianStudyBot;
using SyrianStudyBot.Data;
using SyrianStudyBot.GlobalExceptionHanndler;


var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"),
        o => o.UseVector());
});

#region Exception Handler

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    
#endregion

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Services
#region Services
builder.Services.AddCoreServices();
builder.Services.AddChatService();
// Optional: enable Telegram bot support only if needed
// builder.Services.AddTelegramServices();
#endregion

// Chat Provider is handled in AddChatService

// Optional Telegram worker registration is available in AddTelegramServices().

var app = builder.Build();

app.UseExceptionHandler();


// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();