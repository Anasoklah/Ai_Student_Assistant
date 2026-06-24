using SyrianStudyBot.Domain;

namespace SyrianStudyBot.Common.Services;

public interface IUsageTrackingService
{
    void ResetMessageCounterIfNeeded(ApplicationUser user);
    void ResetUploadCounterIfNeeded(ApplicationUser user);
    Task UpsertDailyUsageAsync(Guid userId, CancellationToken cancellationToken);
    Task UpsertUploadUsageAsync(Guid userId, CancellationToken cancellationToken);
}
