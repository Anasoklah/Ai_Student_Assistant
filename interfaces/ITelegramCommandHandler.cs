namespace SyrianStudyBot.interfaces;

public interface ITelegramCommandHandler
{
    Task<string> HandleAsync(string text, long userId, CancellationToken cancellationToken);
}
