using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password,
    [MaxLength(100)] string? FirstName,
    [MaxLength(100)] string? LastName);

public sealed record RegisterResponse(string UserId, string Email, string Message);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record RefreshTokenRequest(string? RefreshToken);

public sealed record RevokeTokenRequest(string? RefreshToken);

public sealed record VerifyEmailRequest(
    [Required] string UserId,
    [Required] string Token);

public sealed record EmailRequest([Required, EmailAddress] string Email);

public sealed record ResetPasswordRequest(
    [Required] string UserId,
    [Required] string Token,
    [Required, MinLength(8), MaxLength(128)] string NewPassword);

public sealed record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(8), MaxLength(128)] string NewPassword);

public sealed record AuthUserDto(string Id, string Email, string? FirstName, string? LastName, long? CustomerId,
    IReadOnlyList<string> Roles);

public sealed record AuthResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAt,
    AuthUserDto User,
    string? RefreshToken = null,
    DateTime? RefreshTokenExpiresAt = null);

/// <summary>Внутрішній результат входу чи оновлення: refresh token контролер кладе в HttpOnly cookie.</summary>
public sealed record AuthResult(AuthResponse Response, string RefreshToken, DateTime RefreshTokenExpiresAt);

public sealed record MessageResponse(string Message);
