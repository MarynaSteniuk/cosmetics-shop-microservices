namespace Auth.Api.Email;

/// <summary>
/// Мок поштового сервісу: замість надсилання листа записує посилання в лог (Aspire Dashboard → Structured logs).
/// </summary>
public sealed class LoggingEmailSender : IAuthEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailConfirmationAsync(string email, string link, CancellationToken ct)
    {
        _logger.LogInformation("[MOCK EMAIL] Email confirmation for {Email}: {ConfirmationLink}", email, link);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string link, CancellationToken ct)
    {
        _logger.LogInformation("[MOCK EMAIL] Password reset for {Email}: {ResetLink}", email, link);
        return Task.CompletedTask;
    }
}
