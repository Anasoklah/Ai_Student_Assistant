namespace SyrianStudyBot.Domain.Exceptions;

public class ForbiddenException : DomainException
{
    public ForbiddenException(string message, string? details = null) : base(message, details) { }
    public ForbiddenException(string message, Exception innerException, string? details = null) : base(message, innerException, details) { }
}
