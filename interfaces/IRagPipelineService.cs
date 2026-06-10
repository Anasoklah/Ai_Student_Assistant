namespace SyrianStudyBot.interfaces;

public interface IRagPipelineService
{
    // The main entry point for student questions.
    // question: what the student asked
    // mode: "explain", "summary", or "quiz"
    // subject: optional subject filter (e.g. "Math")
    Task<string> QueryAsync(string question, string mode, string? subject, CancellationToken cancellationToken = default);
}
