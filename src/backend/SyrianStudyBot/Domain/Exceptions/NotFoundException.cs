namespace SyrianStudyBot.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string message, string? details = null) : base(message, details) { }
    public NotFoundException(string message, Exception innerException, string? details = null) : base(message, innerException, details) { }
}
