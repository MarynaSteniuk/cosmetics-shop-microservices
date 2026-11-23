using Auth.Api.Contracts;

namespace Auth.Api.Services;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResult> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct);
    Task<AuthResult> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken ct);
    Task RevokeAsync(string refreshToken, string requesterId, bool requesterIsAdmin, string reason, CancellationToken ct);
    Task LogoutAsync(string userId, string? refreshToken, CancellationToken ct);
    Task VerifyEmailAsync(string userId, string encodedToken, CancellationToken ct);
    Task ResendVerificationAsync(string email, CancellationToken ct);
    Task ForgotPasswordAsync(string email, CancellationToken ct);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct);
    Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct);
    Task<AuthUserDto> GetCurrentUserAsync(string userId, CancellationToken ct);
}
