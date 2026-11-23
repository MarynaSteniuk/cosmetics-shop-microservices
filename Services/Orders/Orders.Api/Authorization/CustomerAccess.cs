using System.Security.Claims;
using Platform.ServiceDefaults.Security;

namespace Orders.Api.Authorization;

/// <summary>
/// Claims у бізнес-логіці: користувач Auth Service бачить і створює лише замовлення свого клієнта (claim customer_id).
/// Admin (Auth Service), admin і manager (Keycloak) мають доступ до всіх замовлень.
/// Токени IdentityServer і Keycloak без customer_id обмежуються лише дозволами матриці.
/// </summary>
public static class CustomerAccess
{
    public const string CustomerIdClaim = "customer_id";

    public static bool CanAccess(ClaimsPrincipal user, long customerId, string authServiceIssuer)
    {
        if (IsStaff(user))
            return true;

        if (user.FindFirst(CustomerIdClaim)?.Value is { } value)
            return long.TryParse(value, out var ownCustomerId) && ownCustomerId == customerId;

        return PlatformAuthentication.IssuerKind(user, authServiceIssuer) != PlatformAuthentication.AuthServiceScheme;
    }

    private static bool IsStaff(ClaimsPrincipal user)
        => user.IsInRole("Admin") || user.IsInRole("admin") || user.IsInRole("manager");
}
