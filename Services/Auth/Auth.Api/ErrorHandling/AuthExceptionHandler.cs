using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.ErrorHandling;

public sealed class AuthExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<AuthExceptionHandler> _logger;

    public AuthExceptionHandler(IProblemDetailsService problemDetails, ILogger<AuthExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        ProblemDetails problem;

        switch (exception)
        {
            case AuthException auth:
                _logger.LogWarning("Auth request {Method} {Path} rejected with {StatusCode}: {Detail}",
                    context.Request.Method, context.Request.Path, auth.StatusCode, auth.Message);
                problem = new ProblemDetails { Status = auth.StatusCode, Title = auth.Title, Detail = auth.Message };
                break;

            case IdentityValidationException validation:
                _logger.LogWarning("Auth request {Method} {Path} failed validation: {Errors}",
                    context.Request.Method, context.Request.Path, string.Join("; ", validation.Errors.Keys));
                problem = new ValidationProblemDetails(validation.Errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred."
                };
                break;

            default:
                return false;
        }

        context.Response.StatusCode = problem.Status!.Value;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
