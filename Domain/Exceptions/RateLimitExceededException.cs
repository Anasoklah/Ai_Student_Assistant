namespace SyrianStudyBot.Domain.Exceptions;

public class RateLimitExceededException : DomainException
{
    public RateLimitExceededException(string message, string? details = null) : base(message, details) { }
    public RateLimitExceededException(string message, Exception innerException, string? details = null) : base(message, innerException, details) { }
}
