namespace Auth.Api.Email;

public interface IAuthEmailSender
{
    Task SendEmailConfirmationAsync(string email, string link, CancellationToken ct);

    Task SendPasswordResetAsync(string email, string link, CancellationToken ct);
}
