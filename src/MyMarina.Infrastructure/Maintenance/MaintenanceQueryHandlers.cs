using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Maintenance;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Maintenance;

public class GetMarinaMaintenanceRequestsQueryHandler(
    AppDbContext db,
    UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetMarinaMaintenanceRequestsQuery, IReadOnlyList<MaintenanceRequestDto>>
{
    public async Task<IReadOnlyList<MaintenanceRequestDto>> HandleAsync(
        GetMarinaMaintenanceRequestsQuery query, CancellationToken ct = default)
    {
        var q = db.MaintenanceRequests
            .Include(r => r.WorkOrder)
            .Where(r => r.MarinaId == query.MarinaId);

        if (!string.IsNullOrEmpty(query.Status) &&
            Enum.TryParse<MaintenanceRequestStatus>(query.Status, out var status))
            q = q.Where(r => r.Status == status);

        var requests = await q.OrderByDescending(r => r.SubmittedAt).ToListAsync(ct);

        var results = new List<MaintenanceRequestDto>(requests.Count);
        foreach (var r in requests)
        {
            var appUser = await userManager.FindByIdAsync(r.BoaterUserId.ToString());
            var name    = appUser is null ? "" : $"{appUser.FirstName} {appUser.LastName}".Trim();
            results.Add(MaintenanceHelper.ToDto(r, name));
        }

        return results;
    }
}

public class GetMaintenanceRequestQueryHandler(
    AppDbContext db,
    UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetMaintenanceRequestQuery, MaintenanceRequestDto?>
{
    public async Task<MaintenanceRequestDto?> HandleAsync(
        GetMaintenanceRequestQuery query, CancellationToken ct = default)
    {
        var r = await db.MaintenanceRequests
            .Include(r => r.WorkOrder)
            .FirstOrDefaultAsync(r => r.MarinaId == query.MarinaId && r.Id == query.RequestId, ct);

        if (r is null) return null;

        var appUser = await userManager.FindByIdAsync(r.BoaterUserId.ToString());
        var name    = appUser is null ? "" : $"{appUser.FirstName} {appUser.LastName}".Trim();
        return MaintenanceHelper.ToDto(r, name);
    }
}

public class GetMyMaintenanceRequestsQueryHandler(
    AppDbContext db,
    IUserContext user,
    UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetMyMaintenanceRequestsQuery, IReadOnlyList<MaintenanceRequestDto>>
{
    public async Task<IReadOnlyList<MaintenanceRequestDto>> HandleAsync(
        GetMyMaintenanceRequestsQuery query, CancellationToken ct = default)
    {
        var requests = await db.MaintenanceRequests
            .Include(r => r.WorkOrder)
            .Where(r => r.BoaterUserId == user.UserId!.Value)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(ct);

        var appUser = await userManager.FindByIdAsync(user.UserId!.Value.ToString());
        var name    = appUser is null ? "" : $"{appUser.FirstName} {appUser.LastName}".Trim();

        return requests.Select(r => MaintenanceHelper.ToDto(r, name)).ToList();
    }
}

public class GetMarinaWorkOrdersQueryHandler(
    AppDbContext db,
    UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetMarinaWorkOrdersQuery, IReadOnlyList<WorkOrderDto>>
{
    public async Task<IReadOnlyList<WorkOrderDto>> HandleAsync(
        GetMarinaWorkOrdersQuery query, CancellationToken ct = default)
    {
        var q = db.WorkOrders.Where(w => w.MarinaId == query.MarinaId);

        if (!string.IsNullOrEmpty(query.Status) &&
            Enum.TryParse<WorkOrderStatus>(query.Status, out var status))
            q = q.Where(w => w.Status == status);

        var orders = await q.OrderByDescending(w => w.CreatedAt).ToListAsync(ct);

        var results = new List<WorkOrderDto>(orders.Count);
        foreach (var w in orders)
        {
            string? assignedName = null;
            if (w.AssignedToUserId.HasValue)
            {
                var staff = await userManager.FindByIdAsync(w.AssignedToUserId.Value.ToString());
                assignedName = staff is null ? null : $"{staff.FirstName} {staff.LastName}".Trim();
            }
            results.Add(MaintenanceHelper.ToDto(w, assignedName));
        }

        return results;
    }
}

public class GetWorkOrderQueryHandler(
    AppDbContext db,
    UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetWorkOrderQuery, WorkOrderDto?>
{
    public async Task<WorkOrderDto?> HandleAsync(
        GetWorkOrderQuery query, CancellationToken ct = default)
    {
        var w = await db.WorkOrders
            .FirstOrDefaultAsync(w => w.MarinaId == query.MarinaId && w.Id == query.WorkOrderId, ct);

        if (w is null) return null;

        string? assignedName = null;
        if (w.AssignedToUserId.HasValue)
        {
            var staff = await userManager.FindByIdAsync(w.AssignedToUserId.Value.ToString());
            assignedName = staff is null ? null : $"{staff.FirstName} {staff.LastName}".Trim();
        }

        return MaintenanceHelper.ToDto(w, assignedName);
    }
}

public class GetMarinaAnnouncementsQueryHandler(AppDbContext db)
    : IQueryHandler<GetMarinaAnnouncementsQuery, IReadOnlyList<AnnouncementDto>>
{
    public async Task<IReadOnlyList<AnnouncementDto>> HandleAsync(
        GetMarinaAnnouncementsQuery query, CancellationToken ct = default)
    {
        var announcements = await db.Announcements
            .Where(a => a.MarinaId == query.MarinaId)
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.PublishedAt ?? a.CreatedAt)
            .ToListAsync(ct);

        return announcements.Select(MaintenanceHelper.ToDto).ToList();
    }
}

public class GetMyAnnouncementsQueryHandler(AppDbContext db, IUserContext user)
    : IQueryHandler<GetMyAnnouncementsQuery, IReadOnlyList<AnnouncementDto>>
{
    public async Task<IReadOnlyList<AnnouncementDto>> HandleAsync(
        GetMyAnnouncementsQuery query, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        // Marinas where the user has a billing account membership
        var customerMarinaIds = await db.BillingAccountMembers
            .Where(m => m.UserId == user.UserId)
            .Join(db.BillingAccounts, m => m.BillingAccountId, b => b.Id,
                (m, b) => b.MarinaId)
            .Distinct()
            .ToListAsync(ct);

        // Marinas where the user has an upcoming or current reservation
        var incomingMarinaIds = await db.Reservations
            .Where(r => r.BoaterUserId == user.UserId
                     && r.DepartsAt > now
                     && r.Status != Domain.Enums.ReservationStatus.Cancelled
                     && r.Status != Domain.Enums.ReservationStatus.Declined)
            .Join(db.Slips, r => r.SlipId, s => s.Id, (r, s) => s.MarinaId)
            .Distinct()
            .ToListAsync(ct);

        var announcements = await db.Announcements
            .Where(a =>
                a.PublishedAt != null &&
                (a.ExpiresAt == null || a.ExpiresAt > now) &&
                (
                    (customerMarinaIds.Contains(a.MarinaId) &&
                        (a.Audience == Domain.Enums.AnnouncementAudience.Customers ||
                         a.Audience == Domain.Enums.AnnouncementAudience.Both))
                    ||
                    (incomingMarinaIds.Contains(a.MarinaId) &&
                        (a.Audience == Domain.Enums.AnnouncementAudience.IncomingBoaters ||
                         a.Audience == Domain.Enums.AnnouncementAudience.Both))
                ))
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.PublishedAt)
            .ToListAsync(ct);

        return announcements.Select(MaintenanceHelper.ToDto).ToList();
    }
}
