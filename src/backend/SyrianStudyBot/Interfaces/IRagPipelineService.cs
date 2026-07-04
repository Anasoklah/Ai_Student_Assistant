using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Interfaces;

public interface IRagPipelineService
{
    Task<string> QueryAsync(string question, ChatMode mode, Subject? subject, string? sectionFilter = null, string? chapterFilter = null, CancellationToken cancellationToken = default);
}
