namespace SyrianStudyBot.Application.Auth.Dtos;

public record ResendVerificationDto
{
    public string Email { get; set; } = string.Empty;
}
