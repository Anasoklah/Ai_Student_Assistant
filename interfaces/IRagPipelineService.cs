using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.interfaces;

public interface IRagPipelineService
{
    // The main entry point for student questions.
    // sectionFilter: optional free-text to restrict search to a specific section/lesson name.
    Task<string> QueryAsync(
        string question,
        ChatMode mode,
        Subject? subject,
        string? chapterFilter = null,
        string? sectionFilter = null,
        CancellationToken cancellationToken = default);
}
