using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace Platform.IdentityServer;

/// <summary>
/// Початкова конфігурація IdentityServer. Під час першого запуску SeedData переносить її в ConfigurationDbContext,
/// далі IdentityServer читає clients і resources з бази IdentityServerDb.
/// </summary>
public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResources.Email()
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new("orders.read", "Read orders"),
        new("orders.write", "Create, update, delete and approve orders"),
        new("products.read", "Read catalog products"),
        new("products.write", "Manage catalog products"),
        new("reviews.read", "Read product reviews"),
        new("payments.read", "Read payments"),
        new("payments.process", "Process and refund payments")
    ];

    /// <summary>Назва ApiResource потрапляє в aud токена; кожен мікросервіс перевіряє свою аудиторію.</summary>
    public static IEnumerable<ApiResource> ApiResources =>
    [
        new("orders_api", "Orders API") { Scopes = { "orders.read", "orders.write" } },
        new("products_api", "Catalog (Products) API") { Scopes = { "products.read", "products.write" } },
        new("reviews_api", "Reviews API") { Scopes = { "reviews.read" } },
        new("payments_api", "Payments API") { Scopes = { "payments.read", "payments.process" } }
    ];

    public static IEnumerable<Client> Clients(IConfiguration configuration) =>
    [
        // Authorization Code для ручного тестування в Postman (confidential client із секретом).
        new Client
        {
            ClientId = "postman",
            ClientName = "Postman (Authorization Code)",
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = false,
            ClientSecrets = { new Secret((configuration["Clients:PostmanSecret"] ?? "postman-secret").Sha256()) },
            RedirectUris = { "https://oauth.pstmn.io/v1/callback" },
            AllowOfflineAccess = true,
            AccessTokenLifetime = 900,
            AllowedScopes =
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                IdentityServerConstants.StandardScopes.Email,
                "orders.read", "orders.write", "products.read", "products.write", "reviews.read", "payments.read"
            }
        },

        // Client Credentials: Aggregator викликає Orders, Catalog і Reviews від свого імені (machine-to-machine).
        new Client
        {
            ClientId = "aggregator_service",
            ClientName = "Platform Aggregator (Client Credentials)",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret((configuration["Clients:AggregatorSecret"] ?? "aggregator-secret").Sha256()) },
            AccessTokenLifetime = 3600,
            AllowedScopes = { "orders.read", "products.read", "reviews.read" }
        },

        // Public client для Swagger UI: секрету немає, тому обов'язковий PKCE.
        new Client
        {
            ClientId = "swagger",
            ClientName = "Swagger UI (Authorization Code + PKCE)",
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RequireClientSecret = false,
            RedirectUris =
            {
                "https://localhost:7206/swagger/oauth2-redirect.html",
                "https://localhost:7098/swagger/oauth2-redirect.html"
            },
            AllowedCorsOrigins = { "https://localhost:7206", "https://localhost:7098" },
            AccessTokenLifetime = 900,
            AllowedScopes =
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                "orders.read", "orders.write", "products.read", "products.write"
            }
        }
    ];
}
