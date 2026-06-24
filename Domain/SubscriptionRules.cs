using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Domain;

public static class SubscriptionRules
{
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
}
