namespace SyrianStudyBot.Features.Auth.Services.Options;

public class RefreshTokenSettings
{
    public int ExpiresInDays { get; set; } = 7;
    public int MaxActiveTokensPerUser { get; set; } = 5;
}
