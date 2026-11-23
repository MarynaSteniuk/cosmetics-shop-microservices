using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Platform.ServiceDefaults.Security;

/// <summary>
/// Спільні частини автентифікації платформи (Лаб. 09). Кожен сервіс приймає токени трьох видавців:
/// Auth Service (частина 1), Duende IdentityServer (частина 2) і Keycloak (частина 3).
/// Схема MultiIssuer читає iss токена й передає його потрібній JwtBearer-схемі.
/// </summary>
public static class PlatformAuthentication
{
    public const string MultiIssuerScheme = "MultiIssuer";
    public const string AuthServiceScheme = "AuthService";
    public const string IdentityServerScheme = "IdentityServer";
    public const string KeycloakScheme = "Keycloak";

    public const string KeycloakServiceName = "keycloak";
    public const string KeycloakRealm = "DevRealmDrivers";
    public const string DefaultAuthServiceIssuer = "cosmetics-auth";
    public const string DefaultIdentityServerAuthority = "https://localhost:5101";

    public const string RoleClaimType = "role";
    public const string ScopeClaimType = "scope";
    public const string SubjectClaimType = "sub";

    public static string AuthServiceIssuer(IConfiguration configuration)
        => configuration["Jwt:Issuer"] ?? DefaultAuthServiceIssuer;

    public static string IdentityServerAuthority(IConfiguration configuration)
        => configuration["IdentityServer:Authority"] ?? DefaultIdentityServerAuthority;

    /// <summary>Вибір схеми за полем iss, без перевірки підпису (підпис перевіряє вже обрана схема).</summary>
    public static string SelectScheme(HttpContext context, string authServiceIssuer)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthServiceScheme;

        var token = header["Bearer ".Length..].Trim();
        var handler = new JsonWebTokenHandler();
        if (!handler.CanReadToken(token))
            return AuthServiceScheme;

        return SchemeForIssuer(handler.ReadJsonWebToken(token).Issuer, authServiceIssuer);
    }

    public static string SchemeForIssuer(string? issuer, string authServiceIssuer)
    {
        if (string.Equals(issuer, authServiceIssuer, StringComparison.Ordinal))
            return AuthServiceScheme;

        if (issuer?.Contains("/realms/", StringComparison.OrdinalIgnoreCase) == true)
            return KeycloakScheme;

        return IdentityServerScheme;
    }

    /// <summary>Видавець уже автентифікованого користувача (за claim iss).</summary>
    public static string IssuerKind(ClaimsPrincipal user, string authServiceIssuer)
        => SchemeForIssuer(user.FindFirst("iss")?.Value, authServiceIssuer);

    /// <summary>Частина 1: перевірка JWT Auth Service — симетричний ключ HMAC-SHA256, issuer, audience, термін дії.</summary>
    public static TokenValidationParameters AuthServiceValidation(IConfiguration configuration)
    {
        var secret = configuration["Jwt:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            throw new InvalidOperationException(
                "Jwt:SecretKey is missing. Run the service through Platform.AppHost (parameter jwt-secret).");

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = AuthServiceIssuer(configuration),
            ValidateAudience = true,
            ValidAudience = configuration["Jwt:Audience"] ?? "cosmetics-platform",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            RoleClaimType = RoleClaimType,
            AuthenticationType = AuthServiceScheme
        };
    }

    /// <summary>Частина 3: ролі realm Keycloak лежать у realm_access.roles; переносимо їх у звичайні claims role.</summary>
    public static JwtBearerEvents KeycloakEvents() => new()
    {
        OnTokenValidated = context =>
        {
            if (context.Principal?.Identity is ClaimsIdentity identity
                && identity.FindFirst("realm_access")?.Value is { Length: > 0 } realmAccess)
            {
                using var json = JsonDocument.Parse(realmAccess);
                if (json.RootElement.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
                {
                    foreach (var role in roles.EnumerateArray())
                    {
                        if (role.GetString() is { } name && !identity.HasClaim(identity.RoleClaimType, name))
                            identity.AddClaim(new Claim(identity.RoleClaimType, name));
                    }
                }
            }

            return Task.CompletedTask;
        }
    };
}
