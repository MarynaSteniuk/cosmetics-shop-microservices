using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Спільні налаштування автентифікації для проєктів платформи (Лаб. 09).
/// Один секрет і одні значення Issuer/Audience для Auth Service, Gateway і мікросервісів.
/// </summary>
public static class PlatformAuthExtensions
{
    public const string AuthServiceIssuer = "cosmetics-auth";
    public const string AuthServiceAudience = "cosmetics-platform";
    public const string KeycloakRealm = "DevRealmDrivers";

    /// <summary>Частина 1: параметри JWT, якими Auth Service підписує, а інші сервіси перевіряють токени.</summary>
    public static IResourceBuilder<ProjectResource> WithAuthServiceJwt(this IResourceBuilder<ProjectResource> project,
        IResourceBuilder<ParameterResource> jwtSecret)
        => project
            .WithEnvironment("Jwt__Issuer", AuthServiceIssuer)
            .WithEnvironment("Jwt__Audience", AuthServiceAudience)
            .WithEnvironment("Jwt__SecretKey", jwtSecret);

    /// <summary>Частина 2: адреса IdentityServer (Authority) для валідації токенів і отримання Client Credentials.</summary>
    public static IResourceBuilder<ProjectResource> WithIdentityServerAuthority(
        this IResourceBuilder<ProjectResource> project, IResourceBuilder<ProjectResource> identityServer)
        => project.WithEnvironment("IdentityServer__Authority", identityServer.GetEndpoint("https"));

    /// <summary>Усі три видавці токенів: Auth Service (частина 1), IdentityServer (частина 2), Keycloak (частина 3).</summary>
    public static IResourceBuilder<ProjectResource> WithPlatformAuth(this IResourceBuilder<ProjectResource> project,
        IResourceBuilder<ParameterResource> jwtSecret,
        IResourceBuilder<ProjectResource> identityServer,
        IResourceBuilder<KeycloakResource> keycloak)
        => project
            .WithAuthServiceJwt(jwtSecret)
            .WithIdentityServerAuthority(identityServer)
            .WithEnvironment("Keycloak__Realm", KeycloakRealm)
            .WithReference(keycloak)
            .WaitFor(keycloak);
}
