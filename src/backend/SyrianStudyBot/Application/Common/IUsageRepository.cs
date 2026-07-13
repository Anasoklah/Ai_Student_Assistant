using SyrianStudyBot.Domain.Entities;

namespace SyrianStudyBot.Application.Common;

/// <summary>
/// Repository for DailyUsageLog database operations.
/// Tracks per-user daily message counts, upload counts, and token usage.
///
/// Replaces direct AppDbContext usage in UsageTrackingService.
/// Note: UsageTrackingService adds DailyUsageLog entries but does NOT call
/// SaveChangesAsync — the caller (ChatUseCase) is responsible for persisting.
/// </summary>
public interface IUsageRepository
{
    // ── DailyUsageLog queries ──

    /// <summary>
    /// Returns today's usage log for a user, or null if no log exists yet today.
    /// </summary>
    Task<DailyUsageLog?> GetTodayLogAsync(Guid userId, CancellationToken ct = default);

    // ── DailyUsageLog commands ──

    /// <summary>
    /// Stages a new DailyUsageLog for insertion. Does not save immediately;
    /// the caller (typically ChatUseCase) calls SaveChangesAsync.
    /// </summary>
    void Add(DailyUsageLog log);

    /// <summary>
    /// Persists all tracked entity changes.
    /// In practice, this is called by the consuming UseCase (e.g., ChatUseCase),
    /// not by UsageTrackingService itself.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
