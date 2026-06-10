using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Data;
using SyrianStudyBot.Domain;
using SyrianStudyBot.interfaces;

namespace SyrianStudyBot.Services;

public class UserSessionService(AppDbContext db) : IUserSessionService
{
    public async Task<UserSession> GetOrCreateAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        var session = await db.UserSessions.FindAsync([telegramUserId], cancellationToken);

        if (session is null)
        {
            // First time this user talks to the bot — create a fresh session
            session = new UserSession { TelegramUserId = telegramUserId };
            db.UserSessions.Add(session);
            await db.SaveChangesAsync(cancellationToken);
        }

        return session;
    }

    public async Task SetSubjectAsync(long telegramUserId, string? subject, CancellationToken cancellationToken = default)
    {
        var session = await GetOrCreateAsync(telegramUserId, cancellationToken);

        // Update the subject filter and mark when they last used the bot
        session.CurrentSubject = subject;
        session.LastActive = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }
}
