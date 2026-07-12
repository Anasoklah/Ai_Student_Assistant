namespace SyrianStudyBot.Features.contracts.services;

public interface IChatService
{
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
}
