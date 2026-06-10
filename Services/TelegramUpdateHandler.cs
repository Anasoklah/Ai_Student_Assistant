using SyrianStudyBot.interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SyrianStudyBot.Services;

public class TelegramUpdateHandler(
    ILogger<TelegramUpdateHandler> logger,
    IServiceScopeFactory scopeFactory) : ITelegramUpdateHandler
{
    public async Task HandleAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        if (!TryGetMessageContext(update, out var message, out var chatId, out var userId))
            return;

        try
        {
            // Command handlers use scoped services such as DbContext.
            // A new scope per Telegram message keeps each request isolated.
            await using var scope = scopeFactory.CreateAsyncScope();
            var commandHandler = scope.ServiceProvider.GetRequiredService<ITelegramCommandHandler>();
            var documentUploadHandler = scope.ServiceProvider.GetRequiredService<ITelegramDocumentUploadHandler>();

            await RouteMessageAsync(
                botClient,
                message,
                chatId,
                userId,
                commandHandler,
                documentUploadHandler,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling message from {UserId}", userId);
            await botClient.SendMessage(chatId, "Something went wrong. Please try again.", cancellationToken: cancellationToken);
        }
    }

    private async Task RouteMessageAsync(
        ITelegramBotClient botClient,
        Message message,
        long chatId,
        long userId,
        ITelegramCommandHandler commandHandler,
        ITelegramDocumentUploadHandler documentUploadHandler,
        CancellationToken cancellationToken)
    {
        // A Telegram Message can contain text, a photo, a document, a voice note, and more.
        // This bot handles documents first because uploaded PDF/TXT files are not text messages.
        if (message.Document is { } document)
        {
            await documentUploadHandler.HandleAsync(
                botClient,
                message,
                document,
                chatId,
                userId,
                cancellationToken);

            return;
        }

        if (message.Text is not { } text)
            return;

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Message from {UserId}: {Text}", userId, text);

        var reply = await commandHandler.HandleAsync(text, userId, cancellationToken);
        await botClient.SendMessage(chatId, reply, cancellationToken: cancellationToken);
    }

    private static bool TryGetMessageContext(
        Update update,
        out Message message,
        out long chatId,
        out long userId)
    {
        message = null!;
        chatId = 0;
        userId = 0;

        // Update is the outer Telegram event object.
        // For normal chat messages, update.Message contains the actual Message.
        if (update.Message is not { From: not null } incomingMessage)
            return false;

        message = incomingMessage;
        chatId = incomingMessage.Chat.Id;
        userId = incomingMessage.From.Id;
        return true;
    }
}
