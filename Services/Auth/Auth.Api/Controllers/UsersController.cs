using Auth.Api.Contracts;
using Auth.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Auth.Api.Controllers;

[ApiController]
[Route("users")]
[Authorize]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private const int MaxPageSize = 100;

    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles, ApplicationDbContext db,
        ILogger<UsersController> logger)
    {
        _users = users;
        _roles = roles;
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType<PagedUsers>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedUsers>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var total = await _users.Users.CountAsync(ct);
        var users = await _users.Users
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = new List<UserDto>(users.Count);
        foreach (var user in users)
            items.Add(await ToDtoAsync(user));

        return Ok(new PagedUsers(items, page, pageSize, total));
    }

    [HttpGet("{id}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(string id)
    {
        if (!IsOwnerOrAdmin(id))
            return Forbid();

        var user = await _users.FindByIdAsync(id);
        return user is null ? UserNotFound(id) : Ok(await ToDtoAsync(user));
    }

    [HttpPut("{id}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Update(string id, UpdateUserRequest request)
    {
        if (!IsOwnerOrAdmin(id))
            return Forbid();

        if (request.CustomerId is not null && !User.IsInRole(AppRoles.Admin))
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden",
                detail: "Only Admin can link a user to a customer");

        var user = await _users.FindByIdAsync(id);
        if (user is null)
            return UserNotFound(id);

        user.FirstName = request.FirstName?.Trim() ?? user.FirstName;
        user.LastName = request.LastName?.Trim() ?? user.LastName;
        user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
        user.CustomerId = request.CustomerId ?? user.CustomerId;

        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
            return IdentityProblem(result);

        _logger.LogInformation("User {UserId} updated by {RequesterId}", id, CurrentUserId());
        return Ok(await ToDtoAsync(user));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(string id)
    {
        if (id == CurrentUserId())
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflict",
                detail: "Admin cannot delete own account");

        var user = await _users.FindByIdAsync(id);
        if (user is null)
            return UserNotFound(id);

        var result = await _users.DeleteAsync(user);
        if (!result.Succeeded)
            return IdentityProblem(result);

        _logger.LogInformation("User {UserId} deleted by {RequesterId}", id, CurrentUserId());
        return NoContent();
    }

    [HttpGet("{id}/refresh-tokens")]
    [ProducesResponseType<IReadOnlyList<RefreshTokenInfoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<RefreshTokenInfoDto>>> GetRefreshTokens(string id,
        CancellationToken ct)
    {
        if (!IsOwnerOrAdmin(id))
            return Forbid();

        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == id)
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        return Ok(tokens
            .Select(t => new RefreshTokenInfoDto(t.Id, t.CreatedAt, t.ExpiresAt, t.CreatedByIp, t.RevokedAt,
                t.RevokedReason, t.IsActive))
            .ToList());
    }

    [HttpGet("{id}/roles")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetRoles(string id)
    {
        if (!IsOwnerOrAdmin(id))
            return Forbid();

        var user = await _users.FindByIdAsync(id);
        return user is null ? UserNotFound(id) : Ok((await _users.GetRolesAsync(user)).ToList());
    }

    [HttpPost("{id}/roles")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignRole(string id, AssignRoleRequest request)
    {
        if (!await _roles.RoleExistsAsync(request.Role))
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Role not found",
                detail: $"Role '{request.Role}' does not exist. Available: {string.Join(", ", AppRoles.All)}");

        var user = await _users.FindByIdAsync(id);
        if (user is null)
            return UserNotFound(id);

        if (await _users.IsInRoleAsync(user, request.Role))
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflict",
                detail: $"User already has role '{request.Role}'");

        var result = await _users.AddToRoleAsync(user, request.Role);
        if (!result.Succeeded)
            return IdentityProblem(result);

        _logger.LogInformation("Role {Role} assigned to user {UserId} by {RequesterId}", request.Role, id, CurrentUserId());
        return NoContent();
    }

    [HttpDelete("{id}/roles/{role}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(string id, string role)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null)
            return UserNotFound(id);

        if (!await _users.IsInRoleAsync(user, role))
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Role not assigned",
                detail: $"User does not have role '{role}'");

        var result = await _users.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
            return IdentityProblem(result);

        _logger.LogInformation("Role {Role} removed from user {UserId} by {RequesterId}", role, id, CurrentUserId());
        return NoContent();
    }

    private string? CurrentUserId() => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    private bool IsOwnerOrAdmin(string id) => id == CurrentUserId() || User.IsInRole(AppRoles.Admin);

    private ObjectResult UserNotFound(string id)
        => Problem(statusCode: StatusCodes.Status404NotFound, title: "User not found", detail: $"User {id} not found");

    private ActionResult IdentityProblem(IdentityResult result)
        => ValidationProblem(new ValidationProblemDetails(result.Errors
            .GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray())));

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
        => new(user.Id, user.Email ?? "", user.FirstName, user.LastName, user.PhoneNumber, user.CustomerId,
            user.EmailConfirmed, await _users.IsLockedOutAsync(user), user.CreatedAt, user.LastLoginAt,
            (await _users.GetRolesAsync(user)).ToList());
}
