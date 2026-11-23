using Pulumi;
using Pulumi.Keycloak.OpenId;
using Pulumi.Keycloak.OpenId.Inputs;

namespace Cosmetics.Infra;

/// <summary>
/// Factory pattern (п. 3.4): усі OpenID clients realm створюються тут за типом flow,
/// тож Program.cs описує лише «що» потрібно, а не налаштування кожного клієнта.
/// </summary>
public sealed class KeycloakClientFactory
{
    private readonly Input<string> _realmId;
    private readonly ProviderResource _provider;

    public KeycloakClientFactory(Input<string> realmId, ProviderResource provider)
    {
        _realmId = realmId;
        _provider = provider;
    }

    /// <summary>Resource server (orders-api / products-api / payments-api): Authorization Services, ENFORCING.</summary>
    public Client CreateResourceServer(ResourceServerDefinition definition) =>
        new($"{definition.ClientId}-client", new ClientArgs
        {
            RealmId = _realmId,
            ClientId = definition.ClientId,
            Name = definition.DisplayName,
            Description = $"Resource server for {definition.DisplayName}",
            Enabled = true,
            AccessType = "CONFIDENTIAL",
            StandardFlowEnabled = false,
            DirectAccessGrantsEnabled = false,
            ServiceAccountsEnabled = true,
            Authorization = new ClientAuthorizationArgs
            {
                PolicyEnforcementMode = "ENFORCING",
                DecisionStrategy = "AFFIRMATIVE",
                KeepDefaults = false
            }
        }, Options());

    /// <summary>Authorization Code Flow, confidential client (Postman). Password grant увімкнено лише для тестів.</summary>
    public Client CreateAuthorizationCodeClient(string resourceName, string clientId, string name,
        Input<string> clientSecret, IEnumerable<string> redirectUris, bool allowPasswordGrant = false)
    {
        var args = new ClientArgs
        {
            RealmId = _realmId,
            ClientId = clientId,
            Name = name,
            Enabled = true,
            AccessType = "CONFIDENTIAL",
            ClientSecret = clientSecret,
            StandardFlowEnabled = true,
            DirectAccessGrantsEnabled = allowPasswordGrant,
            ServiceAccountsEnabled = false,
            WebOrigins = { "+" }
        };
        foreach (var uri in redirectUris)
            args.ValidRedirectUris.Add(uri);

        return new Client(resourceName, args, Options());
    }

    /// <summary>Authorization Code + PKCE (S256), public client без секрету (Swagger UI).</summary>
    public Client CreatePkceClient(string resourceName, string clientId, string name,
        IEnumerable<string> redirectUris, IEnumerable<string> webOrigins)
    {
        var args = new ClientArgs
        {
            RealmId = _realmId,
            ClientId = clientId,
            Name = name,
            Enabled = true,
            AccessType = "PUBLIC",
            StandardFlowEnabled = true,
            DirectAccessGrantsEnabled = false,
            PkceCodeChallengeMethod = "S256"
        };
        foreach (var uri in redirectUris)
            args.ValidRedirectUris.Add(uri);
        foreach (var origin in webOrigins)
            args.WebOrigins.Add(origin);

        return new Client(resourceName, args, Options());
    }

    /// <summary>Client Credentials Flow (machine-to-machine, service account), наприклад Aggregator.</summary>
    public Client CreateClientCredentialsClient(string resourceName, string clientId, string name, Input<string> clientSecret) =>
        new(resourceName, new ClientArgs
        {
            RealmId = _realmId,
            ClientId = clientId,
            Name = name,
            Enabled = true,
            AccessType = "CONFIDENTIAL",
            ClientSecret = clientSecret,
            StandardFlowEnabled = false,
            DirectAccessGrantsEnabled = false,
            ServiceAccountsEnabled = true
        }, Options());

    private CustomResourceOptions Options() => new() { Provider = _provider };
}
