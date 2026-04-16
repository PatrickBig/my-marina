using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.Maintenance;

/// <param name="Status">Filter by status. Null = all statuses.</param>
/// <param name="Priority">Filter by priority. Null = all priorities.</param>
public sealed record GetMaintenanceRequestsQuery(
    MaintenanceStatus? Status,
    Priority? Priority);

public sealed record GetMaintenanceRequestQuery(Guid MaintenanceRequestId);

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record MaintenanceRequestDto(
    Guid Id,
    Guid CustomerAccountId,
    string CustomerDisplayName,
    Guid? SlipId,
    string? SlipName,
    Guid? BoatId,
    string? BoatName,
    string Title,
    string Description,
    MaintenanceStatus Status,
    Priority Priority,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ResolvedAt,
    Guid? WorkOrderId);

// ── Handler interfaces ────────────────────────────────────────────────────────

public interface IGetMaintenanceRequestsQueryHandler
    : IQueryHandler<GetMaintenanceRequestsQuery, IReadOnlyList<MaintenanceRequestDto>>;

public interface IGetMaintenanceRequestQueryHandler
    : IQueryHandler<GetMaintenanceRequestQuery, MaintenanceRequestDto?>;
