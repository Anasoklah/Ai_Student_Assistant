namespace SyrianStudyBot.Application.Auth;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string to, string callbackUrl);
    Task SendResetPasswordToken(string to, string callbackUrl);
}
