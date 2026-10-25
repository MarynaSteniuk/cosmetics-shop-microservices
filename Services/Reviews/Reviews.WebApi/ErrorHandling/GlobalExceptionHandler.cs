using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Reviews.Application.Common.Exceptions;
using Reviews.Domain.Exceptions;

namespace Reviews.WebApi.ErrorHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetails, IHostEnvironment environment,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _environment = environment;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            DomainException => (StatusCodes.Status400BadRequest, "Business rule violation"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey }
                => (StatusCodes.Status409Conflict, "Duplicate key"),
            MongoWriteException => (StatusCodes.Status400BadRequest, "Write rejected by MongoDB"),
            MongoConnectionException => (StatusCodes.Status503ServiceUnavailable, "MongoDB is unavailable"),
            TimeoutException => (StatusCodes.Status503ServiceUnavailable, "MongoDB is unavailable"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        if (status >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Request {Method} {Path} failed with {StatusCode}",
                http.Request.Method, http.Request.Path, status);
        else
            _logger.LogWarning("Request {Method} {Path} failed with {StatusCode}: {Reason}",
                http.Request.Method, http.Request.Path, status, exception.Message);

        ProblemDetails problem = exception is ValidationException validation
            ? new HttpValidationProblemDetails(validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
            : new ProblemDetails();

        problem.Status = status;
        problem.Title = title;
        problem.Detail = status >= StatusCodes.Status500InternalServerError && !_environment.IsDevelopment()
            ? "An unexpected error occurred"
            : exception is ValidationException ? "One or more validation errors occurred" : exception.Message;

        if (_environment.IsDevelopment())
            problem.Extensions["exceptionType"] = exception.GetType().Name;

        http.Response.StatusCode = status;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = exception,
            ProblemDetails = problem
        });
    }
}
