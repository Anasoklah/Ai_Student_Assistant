using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Domain;

public static class SubscriptionRules
{
    private const long Megabyte = 1024 * 1024;

    public static int GetDailyMessageLimit(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free => 10,
        SubscriptionTier.Pro => 500,
        SubscriptionTier.Ultra => 2000,
        _ => 10
    };

    public static int GetMonthlyUploadLimit(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free => 0,
        SubscriptionTier.Pro => 10,
        SubscriptionTier.Ultra => 100,
        _ => 0
    };

    public static bool CanUpload(SubscriptionTier tier) => GetMonthlyUploadLimit(tier) > 0;

    public static long GetMaxUploadFileSizeBytes(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free => 0,
        SubscriptionTier.Pro => 10 * Megabyte,
        SubscriptionTier.Ultra => 50 * Megabyte,
        _ => 0
    };
}
