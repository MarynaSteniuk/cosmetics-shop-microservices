using Pulumi;
using Pulumi.Keycloak.OpenId;

namespace Cosmetics.Infra;

/// <summary>
/// Extension methods (п. 3.4): default scopes клієнтів і protocol mappers client scopes —
/// audience mapper (aud = resource server) і mapper claim "permissions".
/// </summary>
public static class KeycloakClientExtensions
{
    /// <summary>Вбудовані scopes Keycloak; "basic" обов'язковий у Keycloak 25+, без нього в токені немає sub.</summary>
    public static readonly IReadOnlyList<string> BuiltInDefaultScopes = ["basic", "profile", "email", "roles", "web-origins", "acr"];

    /// <summary>Призначає клієнту вбудовані scopes і scopes API (orders_mgmt, products_mgmt, payments_mgmt).</summary>
    public static Client WithDefaultScopes(this Client client, string name, Input<string> realmId,
        IEnumerable<ClientScope> apiScopes, ProviderResource provider)
    {
        var scopes = new InputList<string>();
        foreach (var scope in BuiltInDefaultScopes)
            scopes.Add(scope);
        foreach (var apiScope in apiScopes)
            scopes.Add(apiScope.Name);

        _ = new ClientDefaultScopes($"{name}-default-scopes", new ClientDefaultScopesArgs
        {
            RealmId = realmId,
            ClientId = client.Id,
            DefaultScopes = scopes
        }, new CustomResourceOptions { Provider = provider });

        return client;
    }

    /// <summary>Audience mapper: токен із цим scope отримує aud = clientId resource server.</summary>
    public static ClientScope WithAudienceMapper(this ClientScope clientScope, ResourceServerDefinition server,
        Client resourceServer, Input<string> realmId, ProviderResource provider)
    {
        _ = new AudienceProtocolMapper($"{server.ClientScopeName}-audience", new AudienceProtocolMapperArgs
        {
            RealmId = realmId,
            ClientScopeId = clientScope.Id,
            Name = $"{server.ClientId}-audience",
            IncludedClientAudience = server.ClientId,
            AddToAccessToken = true,
            AddToIdToken = false
        }, new CustomResourceOptions { Provider = provider, DependsOn = { resourceServer } });

        return clientScope;
    }

    /// <summary>Protocol mapper: клієнтські ролі resource server → claim "permissions" звичайного access token.</summary>
    public static ClientScope WithPermissionsMapper(this ClientScope clientScope, ResourceServerDefinition server,
        Client resourceServer, Input<string> realmId, ProviderResource provider)
    {
        _ = new UserClientRoleProtocolMapper($"{server.ClientScopeName}-permissions", new UserClientRoleProtocolMapperArgs
        {
            RealmId = realmId,
            ClientScopeId = clientScope.Id,
            Name = $"{server.ClientId}-permissions",
            ClaimName = "permissions",
            ClientIdForRoleMappings = server.ClientId,
            Multivalued = true,
            AddToAccessToken = true,
            AddToIdToken = false,
            AddToUserinfo = false
        }, new CustomResourceOptions { Provider = provider, DependsOn = { resourceServer } });

        return clientScope;
    }
}
