using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SyrianStudyBot.Domain.Exceptions;

namespace SyrianStudyBot.GlobalExceptionHanndler;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> _logger,
    IProblemDetailsService _problemDetailService,
    IWebHostEnvironment _env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = MapException(exception);

        var isClientError = statusCode < 500;
        var logLevel = isClientError ? LogLevel.Warning : LogLevel.Error;
        _logger.Log(logLevel, exception, "Request {Method} {Path} responded {StatusCode}: {Title}",
            httpContext.Request.Method, httpContext.Request.Path, statusCode, title);

        httpContext.Response.StatusCode = statusCode;

        var detail = ResolveDetail(exception, statusCode);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Instance = httpContext.Request.Path,
            Detail = detail
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        if (exception is ValidationException validationEx && validationEx.Errors is not null)
        {
            problemDetails.Extensions["errors"] = validationEx.Errors;
        }

        return await _problemDetailService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });
    }

    private string ResolveDetail(Exception exception, int statusCode)
    {
        if (_env.IsDevelopment())
            return exception.Message;

        if (statusCode >= 500)
            return "An internal error occurred. Please try again later.";

        return exception is DomainException
            ? exception.Message
            : "An error occurred processing your request.";
    }

    private static (int StatusCode, string Title) MapException(Exception exception)
    => exception switch
    {
        NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
        UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
        ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
        BadRequestException => (StatusCodes.Status400BadRequest, "Bad Request"),
        ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
        RateLimitExceededException => (StatusCodes.Status429TooManyRequests, "Too Many Requests"),
        ValidationException => (StatusCodes.Status422UnprocessableEntity, "Validation Failed"),

        ArgumentNullException or KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
        InvalidOperationException => (StatusCodes.Status400BadRequest, "Bad Request"),
        _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
    };
}
