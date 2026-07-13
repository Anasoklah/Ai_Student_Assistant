using SyrianStudyBot.Domain.Enums;

namespace SyrianStudyBot.Application.Profile.Dtos;

public class ProfileResponseDto
{
    public Guid Id { get; init; }
    public string? Email { get; init; }
    public string? FullName { get; init; }
    public string? PhoneNumber { get; init; }
    public GradeLevel? GradeLevel { get; init; }
    public string? PreferredLanguage { get; init; }
    public SubscriptionTier SubscriptionTier { get; init; }
    public DateTime? SubscriptionExpiresAt { get; init; }
    public int MessagesToday { get; init; }
    public int DailyMessageLimit { get; init; }
    public int UploadsThisMonth { get; init; }
    public int MonthlyUploadLimit { get; init; }
}

public class UpdateProfileRequestDto
{
    public string? FullName { get; init; }
    public string? PhoneNumber { get; init; }
    public GradeLevel? GradeLevel { get; init; }
    public string? PreferredLanguage { get; init; }
}
