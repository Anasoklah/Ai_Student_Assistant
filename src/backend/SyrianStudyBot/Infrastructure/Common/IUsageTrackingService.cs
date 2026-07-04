using SyrianStudyBot.Domain;
using SyrianStudyBot.Domain.Entities;

namespace SyrianStudyBot.Infrastructure.Common;

public interface IUsageTrackingService
{
    void ResetMessageCounterIfNeeded(ApplicationUser user);
    void ResetUploadCounterIfNeeded(ApplicationUser user);
    Task UpsertDailyUsageAsync(Guid userId, CancellationToken cancellationToken);
    Task UpsertUploadUsageAsync(Guid userId, CancellationToken cancellationToken);
}
