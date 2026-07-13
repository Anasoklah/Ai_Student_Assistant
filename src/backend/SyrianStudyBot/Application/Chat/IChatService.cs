namespace SyrianStudyBot.Application.Chat;

public interface IChatService
{
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
}
