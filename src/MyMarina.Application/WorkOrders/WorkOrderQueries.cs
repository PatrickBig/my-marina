using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.WorkOrders;

/// <param name="Status">Filter by status. Null = all statuses.</param>
/// <param name="AssignedToUserId">Filter by assigned staff member. Null = all.</param>
public sealed record GetWorkOrdersQuery(
    WorkOrderStatus? Status,
    Guid? AssignedToUserId);

public sealed record GetWorkOrderQuery(Guid WorkOrderId);

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record WorkOrderDto(
    Guid Id,
    Guid? MaintenanceRequestId,
    string? MaintenanceRequestTitle,
    string Title,
    string Description,
    Guid? AssignedToUserId,
    string? AssignedToName,
    WorkOrderStatus Status,
    Priority Priority,
    DateOnly? ScheduledDate,
    DateTimeOffset? CompletedAt,
    string? Notes,
    DateTimeOffset CreatedAt);

// ── Handler interfaces ────────────────────────────────────────────────────────

public interface IGetWorkOrdersQueryHandler : IQueryHandler<GetWorkOrdersQuery, IReadOnlyList<WorkOrderDto>>;
public interface IGetWorkOrderQueryHandler : IQueryHandler<GetWorkOrderQuery, WorkOrderDto?>;
