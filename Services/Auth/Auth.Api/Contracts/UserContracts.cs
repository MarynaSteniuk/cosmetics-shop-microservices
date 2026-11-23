using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public sealed record UserDto(
    string Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    long? CustomerId,
    bool EmailConfirmed,
    bool IsLockedOut,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    IReadOnlyList<string> Roles);

/// <summary>CustomerId може змінювати лише Admin: так користувач не прив'яже себе до чужих замовлень.</summary>
public sealed record UpdateUserRequest(
    [MaxLength(100)] string? FirstName,
    [MaxLength(100)] string? LastName,
    [Phone, MaxLength(32)] string? PhoneNumber,
    [Range(1, long.MaxValue)] long? CustomerId);

public sealed record AssignRoleRequest([Required] string Role);

public sealed record RefreshTokenInfoDto(
    Guid Id,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    string? CreatedByIp,
    DateTime? RevokedAt,
    string? RevokedReason,
    bool IsActive);

public sealed record PagedUsers(IReadOnlyList<UserDto> Items, int Page, int PageSize, int TotalCount);
