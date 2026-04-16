using MyMarina.Application.Abstractions;

namespace MyMarina.Application.Platform;

/// <param name="TenantId">Filter to a specific tenant. Null = all tenants.</param>
/// <param name="UserId">Filter to a specific user. Null = all users.</param>
/// <param name="Action">Substring filter on the action string. Null = all actions.</param>
/// <param name="EntityType">Filter by entity type. Null = all types.</param>
/// <param name="From">Inclusive lower bound on Timestamp.</param>
/// <param name="To">Inclusive upper bound on Timestamp.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Records per page (max 100).</param>
public sealed record GetAuditLogsQuery(
    Guid? TenantId,
    Guid? UserId,
    string? Action,
    string? EntityType,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page = 1,
    int PageSize = 50);

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record AuditLogDto(
    Guid Id,
    Guid? TenantId,
    string? TenantName,
    Guid UserId,
    string UserEmail,
    string Action,
    string EntityType,
    Guid EntityId,
    string? Before,
    string? After,
    string? IpAddress,
    DateTimeOffset Timestamp);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

// ── Handler interfaces ────────────────────────────────────────────────────────

public interface IGetAuditLogsQueryHandler
    : IQueryHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>;
