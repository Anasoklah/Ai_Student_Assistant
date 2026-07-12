using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;
using SyrianStudyBot.Features.contracts.repositories;

namespace SyrianStudyBot.Infrastructure.Common;

/// <summary>
/// Tracks per-user daily usage metrics (message counts, upload counts).
/// All database operations go through IUsageRepository.
/// 
/// Note: This service typically only calls Add() without SaveChangesAsync.
/// The consuming UseCase (e.g., ChatUseCase) is responsible for the final
/// SaveChangesAsync, since it may also be persisting other entities
/// (messages, session updates) in the same transaction.
/// </summary>
public class UsageTrackingService : IUsageTrackingService
{
    private readonly IUsageRepository _usageRepo;

    public UsageTrackingService(IUsageRepository usageRepo)
    {
        _usageRepo = usageRepo;
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
        var usage = await _usageRepo.GetTodayLogAsync(userId, cancellationToken);

        if (usage is null)
        {
            _usageRepo.Add(new DailyUsageLog
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
        var usage = await _usageRepo.GetTodayLogAsync(userId, cancellationToken);

        if (usage is null)
        {
            _usageRepo.Add(new DailyUsageLog
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
