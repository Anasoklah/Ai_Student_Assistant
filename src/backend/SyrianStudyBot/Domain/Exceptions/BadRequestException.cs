namespace SyrianStudyBot.Domain.Exceptions;

public class BadRequestException : DomainException
{
    public BadRequestException(string message, string? details = null) : base(message, details) { }
    public BadRequestException(string message, Exception innerException, string? details = null) : base(message, innerException, details) { }
}
