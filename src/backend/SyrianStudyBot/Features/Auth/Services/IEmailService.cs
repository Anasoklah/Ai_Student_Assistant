namespace SyrianStudyBot.Features.Auth.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string to, string callbackUrl);
    Task SendResetPasswordToken(string to, string callbackUrl);
}
