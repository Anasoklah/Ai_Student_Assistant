namespace SyrianStudyBot.Domain.Exceptions;

public class ValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    public ValidationException(string message, IReadOnlyDictionary<string, string[]>? errors = null, string? details = null)
        : base(message, details)
    {
        Errors = errors;
    }

    public ValidationException(string message, Exception innerException, IReadOnlyDictionary<string, string[]>? errors = null, string? details = null)
        : base(message, innerException, details)
    {
        Errors = errors;
    }
}
