namespace SyrianStudyBot.Features.Auth.Dtos;

public record ResetPasswordDto
{
    public Guid UserId { get; init; } = default!;
    public string Token { get; init; } = default!;
    public string NewPassword { get; init; } = default!;
}
