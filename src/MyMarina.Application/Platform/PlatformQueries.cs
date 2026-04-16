using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.Platform;

/// <param name="Search">Optional email/name substring search.</param>
/// <param name="TenantId">Filter to users in a specific tenant. Null = all tenants.</param>
/// <param name="Role">Filter by role. Null = all roles.</param>
public sealed record GetPlatformUsersQuery(
    string? Search,
    Guid? TenantId,
    UserRole? Role);

public sealed record GetPlatformUserQuery(Guid UserId);

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record PlatformUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    Guid? TenantId,
    string? TenantName,
    Guid? MarinaId,
    string? MarinaName,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);

// ── Handler interfaces ────────────────────────────────────────────────────────

public interface IGetPlatformUsersQueryHandler
    : IQueryHandler<GetPlatformUsersQuery, IReadOnlyList<PlatformUserDto>>;

public interface IGetPlatformUserQueryHandler
    : IQueryHandler<GetPlatformUserQuery, PlatformUserDto?>;
