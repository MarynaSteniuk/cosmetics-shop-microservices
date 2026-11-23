using Microsoft.AspNetCore.Authorization;

namespace Orders.Api.Authorization;

/// <summary>Вимога: у токені має бути дозвіл (наприклад orders:approve).</summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}
