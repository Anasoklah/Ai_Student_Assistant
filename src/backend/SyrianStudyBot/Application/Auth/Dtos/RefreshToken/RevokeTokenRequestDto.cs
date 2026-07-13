namespace SyrianStudyBot.Application.Auth.Dtos.RefreshToken;

public record RevokeTokenRequestDto
{
    public string RefreshToken { get; init; } = string.Empty;
    public string? Reason { get; init; }
}
