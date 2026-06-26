using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
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
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Services
#region Services
builder.Services.AddCommonServices();
builder.Services.AddCoreServices();
builder.Services.AddChatService();
builder.Services.AddIdentityService();
builder.Services.AddJwtService(builder.Configuration);
builder.Services.AddSettingsServices(builder.Configuration);

#endregion

// Chat Provider is handled in AddChatService

// Optional Telegram worker registration is available in AddTelegramServices().

var app = builder.Build();

await SeedIdentityRolesAsync(app.Services);

// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

static async Task SeedIdentityRolesAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole<Guid>>>();

    foreach (var role in new[] { "Admin", "Student" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole<Guid>(role));
    }
}
