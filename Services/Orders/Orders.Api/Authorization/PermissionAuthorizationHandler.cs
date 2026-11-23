using Microsoft.AspNetCore.Authorization;
using Platform.ServiceDefaults.Security;

namespace Orders.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly string _authServiceIssuer;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(IConfiguration configuration, ILogger<PermissionAuthorizationHandler> logger)
    {
        _authServiceIssuer = PlatformAuthentication.AuthServiceIssuer(configuration);
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        var granted = PermissionGrants.Collect(context.User, _authServiceIssuer);

        if (granted.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        _logger.LogWarning(
            "Permission {Permission} denied for subject {Subject} ({Issuer}); granted: {GrantedPermissions}",
            requirement.Permission,
            context.User.FindFirst(PlatformAuthentication.SubjectClaimType)?.Value,
            PlatformAuthentication.IssuerKind(context.User, _authServiceIssuer),
            granted.Count == 0 ? "none" : string.Join(", ", granted.Order()));

        return Task.CompletedTask;
    }
}
