namespace SyrianStudyBot.Features.Auth.Services.Options;

public class EmailSettings
{
    public string SmtpHost { get; set; } = default!;
    public int SmtpPort { get; set; }
    public string FromEmail { get; set; } = default!;
    public string FromName { get; set; } = default!;
    public string Password { get; set; } = default!;
}
