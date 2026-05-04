using MyMarina.Application.Maintenance;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace MyMarina.Infrastructure.Maintenance;

internal static class MaintenanceHelper
{
    public static MaintenanceRequestDto ToDto(MaintenanceRequest r, string boaterName) => new(
        r.Id, r.MarinaId, r.BoaterUserId, boaterName,
        r.BillingAccountId, r.VesselId, r.SlipId, r.ReservationId,
        r.Title, r.Description,
        r.Status.ToString(), r.Priority.ToString(),
        r.SubmittedAt, r.ResolvedAt,
        r.WorkOrder is null ? null : new WorkOrderSummaryDto(
            r.WorkOrder.Id, r.WorkOrder.Title,
            r.WorkOrder.Status.ToString(), r.WorkOrder.Priority.ToString(),
            r.WorkOrder.ScheduledDate, r.WorkOrder.CompletedAt)
    );

    public static WorkOrderDto ToDto(WorkOrder w, string? assignedToName) => new(
        w.Id, w.MarinaId, w.MaintenanceRequestId,
        w.Title, w.Description,
        w.AssignedToUserId, assignedToName,
        w.Status.ToString(), w.Priority.ToString(),
        w.ScheduledDate, w.CompletedAt, w.Notes, w.CreatedAt
    );

    public static AnnouncementDto ToDto(Announcement a) => new(
        a.Id, a.MarinaId, a.Title, a.Body,
        a.Audience.ToString(),
        a.PublishedAt, a.ExpiresAt, a.IsPinned,
        a.CreatedByUserId, a.CreatedAt
    );
}
