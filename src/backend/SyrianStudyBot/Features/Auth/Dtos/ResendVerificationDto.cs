namespace SyrianStudyBot.Features.Auth.Dtos;

public record ResendVerificationDto
{
    public string Email { get; set; } = string.Empty;
}
