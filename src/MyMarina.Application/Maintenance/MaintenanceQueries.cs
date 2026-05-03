namespace MyMarina.Application.Maintenance;

public sealed record GetMarinaMaintenanceRequestsQuery(
    Guid MarinaId,
    string? Status = null
);

public sealed record GetMaintenanceRequestQuery(
    Guid MarinaId,
    Guid RequestId
);

public sealed record GetMyMaintenanceRequestsQuery;

public sealed record GetMarinaWorkOrdersQuery(
    Guid MarinaId,
    string? Status = null
);

public sealed record GetWorkOrderQuery(
    Guid MarinaId,
    Guid WorkOrderId
);

public sealed record GetMarinaAnnouncementsQuery(
    Guid MarinaId
);

public sealed record GetMyAnnouncementsQuery;
