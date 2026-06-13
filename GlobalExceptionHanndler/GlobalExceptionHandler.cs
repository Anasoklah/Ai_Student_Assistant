using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SyrianStudyBot.GlobalExceptionHanndler;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> _logger,
    IProblemDetailsService _ProblemDetailService
    ) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception 
        , "Unhandled Exception Occured . TaceID : {TraceId}" , httpContext.TraceIdentifier);

        var (statusCode , title) = MapException(exception);

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Instance = httpContext.Request.Path,
            Detail = exception.Message 
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        return await _ProblemDetailService.TryWriteAsync(new ProblemDetailsContext
        {
             HttpContext = httpContext,
              ProblemDetails = problemDetails
        });
    }
    private (int StatusCode , string title) MapException(Exception exception)
    => exception switch
    {
        ArgumentException => (StatusCodes.Status400BadRequest , "Invalid Argument Provided"),
        KeyNotFoundException => (StatusCodes.Status400BadRequest , "Key Not Found "),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized , "UnAuthorized "),
        _ => (StatusCodes.Status500InternalServerError , "An UnExpected Error Occured")
    };
}
