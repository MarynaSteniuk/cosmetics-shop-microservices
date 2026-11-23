namespace Cosmetics.Infra;

/// <summary>Створені ресурси одного resource server (orders-api / products-api / payments-api).</summary>
public sealed record ResourceServer(
    ResourceServerDefinition Definition,
    Pulumi.Keycloak.OpenId.Client Client,
    Pulumi.Keycloak.OpenId.ClientAuthorizationResource Resource,
    IReadOnlyDictionary<string, Pulumi.Keycloak.OpenId.ClientAuthorizationScope> Scopes,
    IReadOnlyDictionary<string, Pulumi.Keycloak.Role> ClientRoles);
