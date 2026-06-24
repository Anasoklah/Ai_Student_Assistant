using Microsoft.EntityFrameworkCore;
using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;

namespace SyrianStudyBot.Common.Services;

public class UsageTrackingService : IUsageTrackingService
{
    private readonly AppDbContext _db;

    public UsageTrackingService(AppDbContext db)
    {
        _db = db;
    }

    public void ResetMessageCounterIfNeeded(ApplicationUser user)
    {
        var today = DateTime.UtcNow.Date;
        if (user.LastMessageReset.Date < today)
        {
            user.MessagesToday = 0;
            user.LastMessageReset = today;
        }
    }

    public void ResetUploadCounterIfNeeded(ApplicationUser user)
    {
        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        if (user.LastUploadReset < currentMonth)
        {
            user.UploadsThisMonth = 0;
            user.LastUploadReset = DateTime.UtcNow;
        }
    }

    public async Task UpsertDailyUsageAsync(Guid userId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var usage = await _db.DailyUsageLogs
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Date == today, cancellationToken);

        if (usage is null)
        {
            _db.DailyUsageLogs.Add(new DailyUsageLog
            {
                UserId = userId,
                Date = today,
                MessageCount = 1
            });
            return;
        }

        usage.MessageCount++;
    }

    public async Task UpsertUploadUsageAsync(Guid userId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var usage = await _db.DailyUsageLogs
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Date == today, cancellationToken);

        if (usage is null)
        {
            _db.DailyUsageLogs.Add(new DailyUsageLog
            {
                UserId = userId,
                Date = today,
                UploadCount = 1
            });
            return;
        }

        usage.UploadCount++;
    }
}
