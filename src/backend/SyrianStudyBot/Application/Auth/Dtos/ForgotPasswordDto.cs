namespace SyrianStudyBot.Application.Auth.Dtos;

public record ForgotPasswordDto
{
    public string Email { get; set; } = string.Empty;
}
