using SyrianStudyBot.Domain;
using SyrianStudyBot.Dtos;

namespace SyrianStudyBot.Common.Mappers;

public static class ProfileMappers
{
    public static ProfileResponseDto MapProfile(ApplicationUser user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        GradeLevel = user.GradeLevel,
        PreferredLanguage = user.PreferredLanguage,
        SubscriptionTier = user.SubscriptionTier,
        SubscriptionExpiresAt = user.SubscriptionExpiresAt,
        MessagesToday = user.MessagesToday,
        DailyMessageLimit = SubscriptionRules.GetDailyMessageLimit(user.SubscriptionTier),
        UploadsThisMonth = user.UploadsThisMonth,
        MonthlyUploadLimit = SubscriptionRules.GetMonthlyUploadLimit(user.SubscriptionTier)
    };
}
