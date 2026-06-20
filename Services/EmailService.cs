using System;
using System.Net;
using System.Net.Mail;
using Authentication.interfaces;

namespace Authentication.Services;

public class EmailService(IConfiguration configuration ,
ILogger<EmailService> logger) : IEmailService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<EmailService> _logger = logger;

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

    private async Task SendEmailAsync(string to , string subject, string htmlBody)
    {
         try
        {
            var smtpHost = _configuration["Email:smtpHost"];
            var smtpPort = int.Parse(_configuration["Email:smtpPort"]!);
            var FromEmail = _configuration["Email:FromEmail"];
            var FromName = _configuration["Email:FromName"];
            var Password = _configuration["Email:Password"];

            var client = new SmtpClient(smtpHost, smtpPort)
            {
                 EnableSsl = true,
                  Credentials = new NetworkCredential(FromEmail , Password)
            };

            var message = new MailMessage
            {
                From = new MailAddress(FromEmail! , "Auth Test"),
                 Subject = subject,
                 Body = htmlBody,
                 IsBodyHtml = true
            };

            message.To.Add(to);
               await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {Email}", to);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", to);
            throw; // Let caller handle retry logic
        }
    }
}
