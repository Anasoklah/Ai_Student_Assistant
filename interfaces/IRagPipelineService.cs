using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.interfaces;

public interface IRagPipelineService
{
    // The main entry point for student questions.
    // question: what the student asked
    Task<string> QueryAsync(string question, ChatMode mode, Subject? subject, CancellationToken cancellationToken = default);
}
