using Telegram.Bot;
using Telegram.Bot.Types;

namespace SyrianStudyBot.interfaces;

public interface ITelegramUpdateHandler
{
    Task HandleAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);
}
