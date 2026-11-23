using System.Security.Claims;
using Auth.Api.Data;

namespace Auth.Api.Services;

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public sealed record GeneratedRefreshToken(string Token, string TokenHash, DateTime ExpiresAt);

public interface IJwtTokenService
{
    AccessToken GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles);

    GeneratedRefreshToken GenerateRefreshToken();

    string HashRefreshToken(string token);

    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
}
