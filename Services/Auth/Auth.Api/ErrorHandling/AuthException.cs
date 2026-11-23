namespace Auth.Api.ErrorHandling;

/// <summary>Очікувана помилка автентифікації з HTTP-статусом (401, 403, 404, 409, 423).</summary>
public sealed class AuthException : Exception
{
    public AuthException(int statusCode, string title, string detail) : base(detail)
    {
        StatusCode = statusCode;
        Title = title;
    }

    public int StatusCode { get; }
    public string Title { get; }
}

/// <summary>Помилки ASP.NET Identity (слабкий пароль, недійсний токен тощо) → 400 ValidationProblem.</summary>
public sealed class IdentityValidationException : Exception
{
    public IdentityValidationException(IDictionary<string, string[]> errors) : base("Identity validation failed")
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }
}
