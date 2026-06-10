namespace SyrianStudyBot.interfaces;

public interface IChatService
{
    // Sends a one-shot request to the LLM.
    // systemPrompt: the instructions (e.g. "You are a tutor, explain clearly...")
    // userMessage: the student's question
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
}
