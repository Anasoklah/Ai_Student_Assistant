using SyrianStudyBot.interfaces;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace SyrianStudyBot;

public class Worker(
    ILogger<Worker> logger,
    IConfiguration configuration,
    ITelegramUpdateHandler updateHandler) : BackgroundService
{
    private readonly TelegramBotClient _botClient = new(
        configuration["Telegram:BotToken"]!,
        new HttpClient(new HttpClientHandler
        {
            // Needed when a VPN or proxy performs SSL inspection.
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
    );

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Bot started.");

        // StartReceiving opens the long-polling loop.
        // Telegram will call updateHandler.HandleAsync every time a new Update arrives.
        _botClient.StartReceiving(
            updateHandler: updateHandler.HandleAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: new ReceiverOptions { AllowedUpdates = [] },
            cancellationToken: stoppingToken
        );

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Telegram polling error");
        return Task.CompletedTask;
    }
}
