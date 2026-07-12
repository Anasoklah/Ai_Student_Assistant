using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Features.contracts.repositories;

namespace SyrianStudyBot.Infrastructure.Persistence.Repositories;

/// <summary>
/// Handles database operations for DailyUsageLog entities.
/// Tracks per-user daily usage metrics (messages, uploads, tokens).
///
/// Note: UsageTrackingService typically only calls Add() without SaveChangesAsync.
/// The consuming UseCase (e.g., ChatUseCase) is responsible for the final SaveChangesAsync.
/// This works because all repositories share the same scoped DbContext.
/// </summary>
public class UsageRepository : IUsageRepository
{
    private readonly AppDbContext _db;

    public UsageRepository(AppDbContext db)
    {
        _db = db;
    }

    // ── DailyUsageLog queries ──

    public async Task<DailyUsageLog?> GetTodayLogAsync(Guid userId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _db.DailyUsageLogs
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Date == today, ct);
    }

    // ── DailyUsageLog commands ──

    public void Add(DailyUsageLog log)
    {
        _db.DailyUsageLogs.Add(log);
    }

    // ── Unit of Work ──

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _db.SaveChangesAsync(ct);
    }
}
