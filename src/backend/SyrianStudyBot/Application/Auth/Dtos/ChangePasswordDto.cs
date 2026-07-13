namespace SyrianStudyBot.Application.Auth.Dtos;

public record ChangePasswordDto
{
    public string OldPassword { get; init; } = default!;
    public string NewPassword { get; init; } = default!;
}
