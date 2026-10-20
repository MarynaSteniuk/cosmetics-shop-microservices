using Catalog.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.ErrorHandling;

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
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            FluentValidation.ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            DomainException => (StatusCodes.Status409Conflict, "Business rule violation"),
            DbUpdateException => (StatusCodes.Status409Conflict, "The change conflicts with existing data"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Request {RequestPath} failed", http.Request.Path);
        else
            _logger.LogWarning("Request {RequestPath} failed with {StatusCode}: {Reason}",
                http.Request.Path, status, exception.Message);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError && !_environment.IsDevelopment()
                ? "An unexpected error occurred"
                : exception.Message
        };

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
