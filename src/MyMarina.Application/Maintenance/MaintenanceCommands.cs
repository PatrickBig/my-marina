namespace MyMarina.Application.Maintenance;

// MaintenanceRequest
public sealed record SubmitMaintenanceRequestCommand(
    Guid MarinaId,
    Guid? BillingAccountId,
    Guid? VesselId,
    Guid? SlipId,
    Guid? ReservationId,
    string Title,
    string Description,
    string Priority
);

public sealed record UpdateMaintenanceRequestCommand(
    Guid MarinaId,
    Guid RequestId,
    string Status,
    string Priority
);

// WorkOrder
public sealed record CreateWorkOrderCommand(
    Guid MarinaId,
    Guid? MaintenanceRequestId,
    string Title,
    string Description,
    Guid? AssignedToUserId,
    string Priority,
    DateOnly? ScheduledDate
);

public sealed record UpdateWorkOrderCommand(
    Guid MarinaId,
    Guid WorkOrderId,
    string Title,
    string Description,
    Guid? AssignedToUserId,
    string Status,
    string Priority,
    DateOnly? ScheduledDate,
    string? Notes
);

// Announcement
public sealed record CreateAnnouncementCommand(
    Guid MarinaId,
    string Title,
    string Body,
    string Audience,
    bool IsPinned,
    DateTimeOffset? ExpiresAt
);

public sealed record UpdateAnnouncementCommand(
    Guid MarinaId,
    Guid AnnouncementId,
    string Title,
    string Body,
    string Audience,
    bool IsPinned,
    DateTimeOffset? ExpiresAt
);

public sealed record PublishAnnouncementCommand(
    Guid MarinaId,
    Guid AnnouncementId
);

public sealed record DeleteAnnouncementCommand(
    Guid MarinaId,
    Guid AnnouncementId
);
