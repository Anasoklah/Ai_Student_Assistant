namespace SyrianStudyBot.Domain.Exceptions;

public class ConflictException : DomainException
{
    public ConflictException(string message, string? details = null) : base(message, details) { }
    public ConflictException(string message, Exception innerException, string? details = null) : base(message, innerException, details) { }
}
