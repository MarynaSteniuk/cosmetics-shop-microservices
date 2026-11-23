using System.Text;
using Auth.Api.Contracts;
using Auth.Api.Data;
using Auth.Api.Email;
using Auth.Api.ErrorHandling;
using Auth.Api.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Api.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly ApplicationDbContext _db;
    private readonly IJwtTokenService _tokens;
    private readonly IAuthEmailSender _email;
    private readonly AuthOptions _options;
    private readonly ILogger<AuthService> _logger;

    public AuthService(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn,
        ApplicationDbContext db, IJwtTokenService tokens, IAuthEmailSender email, IOptions<AuthOptions> options,
        ILogger<AuthService> logger)
    {
        _users = users;
        _signIn = signIn;
        _db = db;
        _tokens = tokens;
        _email = email;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        if (await _users.FindByEmailAsync(request.Email) is not null)
            throw new AuthException(StatusCodes.Status409Conflict, "Email already registered",
                "A user with this email already exists");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName?.Trim(),
            LastName = request.LastName?.Trim()
        };

        EnsureSucceeded(await _users.CreateAsync(user, request.Password));
        EnsureSucceeded(await _users.AddToRoleAsync(user, AppRoles.User));

        await SendConfirmationAsync(user, ct);

        _logger.LogInformation("User {UserId} registered with role {Role}, email confirmation sent", user.Id, AppRoles.User);
        return new RegisterResponse(user.Id, request.Email,
            "Registration completed. Confirm your email using the link from the message (see auth-api logs).");
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(request.Email);
        if (user is null)
        {
            _logger.LogWarning("Login failed: unknown email");
            throw InvalidCredentials();
        }

        var result = await _signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            var lockoutEnd = await _users.GetLockoutEndDateAsync(user);
            _logger.LogWarning("Login rejected for user {UserId}: locked out until {LockoutEnd}", user.Id, lockoutEnd);
            throw new AuthException(StatusCodes.Status423Locked, "Account locked",
                $"Too many failed attempts. Try again after {lockoutEnd:yyyy-MM-dd HH:mm} UTC");
        }

        if (result.IsNotAllowed)
        {
            _logger.LogWarning("Login rejected for user {UserId}: email not confirmed", user.Id);
            throw new AuthException(StatusCodes.Status403Forbidden, "Email not confirmed",
                "Confirm your email before signing in");
        }

        if (!result.Succeeded)
        {
            _logger.LogWarning("Login failed for user {UserId}: wrong password, {FailedCount} failed attempts",
                user.Id, await _users.GetAccessFailedCountAsync(user));
            throw InvalidCredentials();
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        _logger.LogInformation("User {UserId} signed in", user.Id);
        return await IssueTokensAsync(user, ipAddress, ct);
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken ct)
    {
        var stored = await FindRefreshTokenAsync(refreshToken, ct)
                     ?? throw new AuthException(StatusCodes.Status401Unauthorized, "Invalid refresh token",
                         "Refresh token is unknown");

        if (stored.IsRevoked)
        {
            // Повторне використання вже замінений токен = ймовірна крадіжка: відкликаємо всю «родину» токенів.
            var revoked = await RevokeAllAsync(stored.UserId, "Reuse of revoked token detected", ct);
            _logger.LogWarning(
                "Refresh token reuse detected for user {UserId}: {RevokedCount} active tokens revoked", stored.UserId, revoked);
            throw new AuthException(StatusCodes.Status401Unauthorized, "Invalid refresh token",
                "Refresh token was already used. All sessions were revoked, sign in again");
        }

        if (stored.IsExpired)
            throw new AuthException(StatusCodes.Status401Unauthorized, "Invalid refresh token", "Refresh token expired");

        var user = await _users.FindByIdAsync(stored.UserId)
                   ?? throw new AuthException(StatusCodes.Status401Unauthorized, "Invalid refresh token", "User not found");

        if (await _users.IsLockedOutAsync(user))
            throw new AuthException(StatusCodes.Status423Locked, "Account locked", "The account is locked");

        var next = _tokens.GenerateRefreshToken();
        stored.Revoke("Rotated", next.TokenHash);

        var result = await IssueTokensAsync(user, ipAddress, ct, next);
        _logger.LogInformation("Refresh token rotated for user {UserId}", user.Id);
        return result;
    }

    public async Task RevokeAsync(string refreshToken, string requesterId, bool requesterIsAdmin, string reason,
        CancellationToken ct)
    {
        var stored = await FindRefreshTokenAsync(refreshToken, ct)
                     ?? throw new AuthException(StatusCodes.Status404NotFound, "Token not found", "Refresh token is unknown");

        if (stored.UserId != requesterId && !requesterIsAdmin)
            throw new AuthException(StatusCodes.Status403Forbidden, "Forbidden", "You can revoke only your own tokens");

        if (!stored.IsActive)
            throw new AuthException(StatusCodes.Status409Conflict, "Token inactive", "Refresh token is already revoked or expired");

        stored.Revoke(reason);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Refresh token {TokenId} of user {UserId} revoked by {RequesterId}: {Reason}",
            stored.Id, stored.UserId, requesterId, reason);
    }

    public async Task LogoutAsync(string userId, string? refreshToken, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken) && await FindRefreshTokenAsync(refreshToken, ct) is { } stored
            && stored.UserId == userId && stored.IsActive)
        {
            stored.Revoke("Logout");
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation("User {UserId} signed out", userId);
    }

    public async Task VerifyEmailAsync(string userId, string encodedToken, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(userId)
                   ?? throw new AuthException(StatusCodes.Status404NotFound, "User not found", "User not found");

        // Одноразовість: підтверджений email не можна підтвердити тим самим посиланням ще раз.
        if (user.EmailConfirmed)
            throw new AuthException(StatusCodes.Status409Conflict, "Link already used", "Email is already confirmed");

        EnsureSucceeded(await _users.ConfirmEmailAsync(user, DecodeToken(encodedToken)));
        await _users.UpdateSecurityStampAsync(user);

        _logger.LogInformation("Email confirmed for user {UserId}", user.Id);
    }

    public async Task ResendVerificationAsync(string email, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(email);
        if (user is null || user.EmailConfirmed)
        {
            _logger.LogInformation("Resend verification requested for unknown or confirmed email, nothing sent");
            return;
        }

        await SendConfirmationAsync(user, ct);
        _logger.LogInformation("Email confirmation re-sent to user {UserId}", user.Id);
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(email);
        if (user is null || !user.EmailConfirmed)
        {
            _logger.LogInformation("Password reset requested for unknown or unconfirmed email, nothing sent");
            return;
        }

        var token = EncodeToken(await _users.GeneratePasswordResetTokenAsync(user));
        var link = $"{_options.PublicBaseUrl.TrimEnd('/')}/auth/reset-password?userId={Uri.EscapeDataString(user.Id)}&token={token}";
        await _email.SendPasswordResetAsync(user.Email!, link, ct);

        _logger.LogInformation("Password reset link sent to user {UserId}", user.Id);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(request.UserId)
                   ?? throw new AuthException(StatusCodes.Status404NotFound, "User not found", "User not found");

        // ResetPasswordAsync змінює security stamp, тож той самий токен вдруге вже недійсний (одноразовість).
        EnsureSucceeded(await _users.ResetPasswordAsync(user, DecodeToken(request.Token), request.NewPassword));
        await _users.ResetAccessFailedCountAsync(user);
        await _users.SetLockoutEndDateAsync(user, null);

        var revoked = await RevokeAllAsync(user.Id, "Password reset", ct);
        _logger.LogInformation("Password reset for user {UserId}, {RevokedCount} refresh tokens revoked", user.Id, revoked);
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(userId)
                   ?? throw new AuthException(StatusCodes.Status404NotFound, "User not found", "User not found");

        EnsureSucceeded(await _users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword));

        var revoked = await RevokeAllAsync(user.Id, "Password changed", ct);
        _logger.LogInformation("Password changed for user {UserId}, {RevokedCount} refresh tokens revoked", user.Id, revoked);
    }

    public async Task<AuthUserDto> GetCurrentUserAsync(string userId, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(userId)
                   ?? throw new AuthException(StatusCodes.Status404NotFound, "User not found", "User not found");

        return ToDto(user, (await _users.GetRolesAsync(user)).ToList());
    }

    private async Task<AuthResult> IssueTokensAsync(ApplicationUser user, string? ipAddress, CancellationToken ct,
        GeneratedRefreshToken? refresh = null)
    {
        var roles = (await _users.GetRolesAsync(user)).ToList();
        var access = _tokens.GenerateAccessToken(user, roles);
        refresh ??= _tokens.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refresh.TokenHash,
            ExpiresAt = refresh.ExpiresAt,
            CreatedByIp = ipAddress
        });
        await _db.SaveChangesAsync(ct);

        var response = new AuthResponse(access.Token, "Bearer", access.ExpiresAt, ToDto(user, roles),
            _options.ExposeRefreshTokenInBody ? refresh.Token : null,
            _options.ExposeRefreshTokenInBody ? refresh.ExpiresAt : null);

        return new AuthResult(response, refresh.Token, refresh.ExpiresAt);
    }

    private Task<RefreshToken?> FindRefreshTokenAsync(string token, CancellationToken ct)
    {
        var hash = _tokens.HashRefreshToken(token);
        return _db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct);
    }

    private async Task<int> RevokeAllAsync(string userId, string reason, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync(ct);

        foreach (var token in active)
            token.Revoke(reason);

        await _db.SaveChangesAsync(ct);
        return active.Count;
    }

    private async Task SendConfirmationAsync(ApplicationUser user, CancellationToken ct)
    {
        var token = EncodeToken(await _users.GenerateEmailConfirmationTokenAsync(user));
        var link = $"{_options.PublicBaseUrl.TrimEnd('/')}/auth/verify-email?userId={Uri.EscapeDataString(user.Id)}&token={token}";
        await _email.SendEmailConfirmationAsync(user.Email!, link, ct);
    }

    private static string EncodeToken(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static string DecodeToken(string encoded)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded));
        }
        catch (FormatException)
        {
            throw new AuthException(StatusCodes.Status400BadRequest, "Invalid token", "Token has invalid format");
        }
    }

    private static AuthException InvalidCredentials()
        => new(StatusCodes.Status401Unauthorized, "Invalid credentials", "Email or password is incorrect");

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (result.Succeeded)
            return;

        var errors = result.Errors
            .GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
        throw new IdentityValidationException(errors);
    }

    private static AuthUserDto ToDto(ApplicationUser user, IReadOnlyList<string> roles)
        => new(user.Id, user.Email ?? "", user.FirstName, user.LastName, user.CustomerId, roles);
}
