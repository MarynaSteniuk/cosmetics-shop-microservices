using Microsoft.AspNetCore.Authorization;

namespace Catalog.Api.Authorization;

/// <summary>
/// [RequirePermission("orders:approve")] — ендпоінт доступний лише з цим дозволом.
/// Назва політики дорівнює назві дозволу; політики реєструються в Program.cs.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission) : base(permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}
