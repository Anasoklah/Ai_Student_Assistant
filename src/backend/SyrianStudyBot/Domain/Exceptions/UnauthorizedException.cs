namespace SyrianStudyBot.Domain.Exceptions;

public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message, string? details = null) : base(message, details) { }
    public UnauthorizedException(string message, Exception innerException, string? details = null) : base(message, innerException, details) { }
}
