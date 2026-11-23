using System.Security.Claims;
using System.Text.Json;
using Platform.ServiceDefaults.Security;

namespace Catalog.Api.Authorization;

/// <summary>
/// Збирає дозволи з токена залежно від видавця:
/// Keycloak — authorization.permissions[].scopes (RPT), claim permissions (protocol mapper) і resource_access.{client}.roles;
/// IdentityServer — scopes (orders.read → orders:read ...);
/// Auth Service — ролі User/Admin.
/// </summary>
public static class PermissionGrants
{
    private static readonly IReadOnlyDictionary<string, string[]> IdentityServerScopes = new Dictionary<string, string[]>
    {
        ["orders.read"] = [Permissions.Orders.Read],
        ["orders.write"] = [Permissions.Orders.Create, Permissions.Orders.Update, Permissions.Orders.Delete, Permissions.Orders.Approve],
        ["products.read"] = [Permissions.Products.Read],
        ["products.write"] = [Permissions.Products.Create, Permissions.Products.Update, Permissions.Products.Delete, Permissions.Products.Publish],
        ["payments.read"] = [Permissions.Payments.Read],
        ["payments.process"] = [Permissions.Payments.Process, Permissions.Payments.Refund]
    };

    private static readonly IReadOnlyDictionary<string, string[]> AuthServiceRoles = new Dictionary<string, string[]>
    {
        ["Admin"] = [.. Permissions.All],
        ["User"] = [Permissions.Orders.Read, Permissions.Orders.Create, Permissions.Products.Read]
    };

    public static HashSet<string> Collect(ClaimsPrincipal user, string authServiceIssuer)
    {
        var granted = new HashSet<string>(StringComparer.Ordinal);

        switch (PlatformAuthentication.IssuerKind(user, authServiceIssuer))
        {
            case PlatformAuthentication.KeycloakScheme:
                AddKeycloakAuthorizationPermissions(user, granted);
                AddKeycloakClientRoles(user, granted);
                AddKeycloakPermissionsClaim(user, granted);
                break;

            case PlatformAuthentication.IdentityServerScheme:
                foreach (var scope in ScopeValues(user))
                {
                    if (IdentityServerScopes.TryGetValue(scope, out var permissions))
                        granted.UnionWith(permissions);
                }
                break;

            case PlatformAuthentication.AuthServiceScheme:
                foreach (var role in user.FindAll(PlatformAuthentication.RoleClaimType).Select(c => c.Value))
                {
                    if (AuthServiceRoles.TryGetValue(role, out var permissions))
                        granted.UnionWith(permissions);
                }
                break;
        }

        return granted;
    }

    /// <summary>RPT Keycloak: "authorization": { "permissions": [ { "rsname": "orders-api", "scopes": ["orders:read"] } ] }.</summary>
    private static void AddKeycloakAuthorizationPermissions(ClaimsPrincipal user, HashSet<string> granted)
    {
        foreach (var claim in user.FindAll("authorization"))
        {
            using var json = TryParse(claim.Value);
            if (json is null || !json.RootElement.TryGetProperty("permissions", out var permissions)
                || permissions.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var permission in permissions.EnumerateArray())
            {
                if (!permission.TryGetProperty("scopes", out var scopes) || scopes.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var scope in scopes.EnumerateArray())
                {
                    if (scope.GetString() is { } value)
                        granted.Add(value);
                }
            }
        }
    }

    /// <summary>Звичайний access token Keycloak: "resource_access": { "orders-api": { "roles": ["orders:read"] } }.</summary>
    private static void AddKeycloakClientRoles(ClaimsPrincipal user, HashSet<string> granted)
    {
        foreach (var claim in user.FindAll("resource_access"))
        {
            using var json = TryParse(claim.Value);
            if (json is null || json.RootElement.ValueKind != JsonValueKind.Object)
                continue;

            foreach (var client in json.RootElement.EnumerateObject())
            {
                if (!client.Value.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var role in roles.EnumerateArray())
                {
                    if (role.GetString() is { } value && value.Contains(':'))
                        granted.Add(value);
                }
            }
        }
    }

    /// <summary>Protocol mapper Pulumi (UserClientRoleProtocolMapper): "permissions": ["orders:read", "products:read"].</summary>
    private static void AddKeycloakPermissionsClaim(ClaimsPrincipal user, HashSet<string> granted)
    {
        foreach (var claim in user.FindAll("permissions"))
        {
            if (claim.Value.Contains(':'))
                granted.Add(claim.Value);
        }
    }

    /// <summary>IdentityServer видає scope як масив (кілька claims), деякі клієнти — як рядок через пробіл.</summary>
    private static IEnumerable<string> ScopeValues(ClaimsPrincipal user)
        => user.FindAll(PlatformAuthentication.ScopeClaimType)
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static JsonDocument? TryParse(string value)
    {
        try
        {
            return JsonDocument.Parse(value);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
