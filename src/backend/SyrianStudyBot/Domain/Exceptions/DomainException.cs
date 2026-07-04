namespace SyrianStudyBot.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public string? Details { get; }

    protected DomainException(string message, string? details = null) : base(message)
    {
        Details = details;
    }

    protected DomainException(string message, Exception innerException, string? details = null) : base(message, innerException)
    {
        Details = details;
    }
}
