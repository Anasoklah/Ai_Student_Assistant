namespace SyrianStudyBot.Features.Auth.Dtos.RefreshToken;

public record TokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime AccessTokenExpiry { get; init; }
    public DateTime RefreshTokenExpiry { get; init; }
    public string? UserName { get; init; }
    public Guid UserId { get; init; }
    public string? Email { get; init; }
}
