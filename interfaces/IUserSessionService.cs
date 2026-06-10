using SyrianStudyBot.Domain;

namespace SyrianStudyBot.interfaces;

public interface IUserSessionService
{
    // Gets the session for a user, or creates a new one if they're new
    Task<UserSession> GetOrCreateAsync(long telegramUserId, CancellationToken cancellationToken = default);

    // Updates the subject filter the user wants to search in (e.g. "Math")
    Task SetSubjectAsync(long telegramUserId, string? subject, CancellationToken cancellationToken = default);
}
