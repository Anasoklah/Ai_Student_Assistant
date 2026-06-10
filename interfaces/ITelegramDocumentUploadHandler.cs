using Telegram.Bot;
using Telegram.Bot.Types;

namespace SyrianStudyBot.interfaces;

public interface ITelegramDocumentUploadHandler
{
    Task HandleAsync(
        ITelegramBotClient botClient,
        Message message,
        Document document,
        long chatId,
        long userId,
        CancellationToken cancellationToken);
}
