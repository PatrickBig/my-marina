namespace MyMarina.Application.Maintenance;

public sealed record MaintenanceRequestDto(
    Guid Id,
    Guid MarinaId,
    Guid BoaterUserId,
    string BoaterName,
    Guid? BillingAccountId,
    Guid? VesselId,
    Guid? SlipId,
    Guid? ReservationId,
    string Title,
    string Description,
    string Status,
    string Priority,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ResolvedAt,
    WorkOrderSummaryDto? WorkOrder
);

public sealed record WorkOrderSummaryDto(
    Guid Id,
    string Title,
    string Status,
    string Priority,
    DateOnly? ScheduledDate,
    DateTimeOffset? CompletedAt
);

public sealed record WorkOrderDto(
    Guid Id,
    Guid MarinaId,
    Guid? MaintenanceRequestId,
    string Title,
    string Description,
    Guid? AssignedToUserId,
    string? AssignedToName,
    string Status,
    string Priority,
    DateOnly? ScheduledDate,
    DateTimeOffset? CompletedAt,
    string? Notes,
    DateTimeOffset CreatedAt
);

public sealed record AnnouncementDto(
    Guid Id,
    Guid MarinaId,
    string Title,
    string Body,
    string Audience,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ExpiresAt,
    bool IsPinned,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt
);
