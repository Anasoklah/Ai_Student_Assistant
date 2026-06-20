using System;

namespace Authentication.interfaces;

public interface IEmailService
{
Task SendVerificationEmailAsync(string to, string callBackUrl);
Task SendResetPasswordToken(string to , string callbackUrl);
}
