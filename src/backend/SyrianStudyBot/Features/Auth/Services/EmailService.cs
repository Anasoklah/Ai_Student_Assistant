using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SyrianStudyBot.Features.Auth.Services.Options;

namespace SyrianStudyBot.Features.Auth.Services;

public class EmailService: IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _settings ;

     public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _logger = logger;
        _settings = configuration.GetSection("Email").Get<EmailSettings>()
            ?? throw new InvalidOperationException("Email settings not configured");
    }
    public async Task SendResetPasswordToken(string to, string callbackUrl)
    {
        var subject = "Reset Password";
       var body = $@"
            <h2>Reset Password</h2>
            <p>Please click on the link below:</p>
            <p><a href='{callbackUrl}' style='padding: 10px 20px; background: #116fd4; color: white; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
            <p>This link expires in 24 hours.</p>";

        await SendEmailAsync(to, subject, body);
    }

    public async Task SendVerificationEmailAsync(string to, string callbackUrl)
    {
       var subject = "Verify Your Email Address";
       var body = $@"
            <h2>Email Verification</h2>
            <p>Please verify your email by clicking the link below:</p>
            <p><a href='{callbackUrl}' style='padding: 10px 20px; background: #116fd4; color: white; text-decoration: none; border-radius: 5px;'>Verify Email</a></p>
            <p>This link expires in 24 hours.</p>
            <p>If you didn't register, ignore this email.</p>";

        await SendEmailAsync(to, subject, body);
    }

    private async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.FromEmail, _settings.Password);
            await client.SendAsync(message);
            _logger.LogInformation("Email sent to {Email}", to);
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}
