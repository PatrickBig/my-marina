using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Platform;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Platform;

public class GetTenantsQueryHandler(AppDbContext db)
    : IQueryHandler<GetTenantsQuery, PagedResult<TenantSummaryDto>>
{
    public async Task<PagedResult<TenantSummaryDto>> HandleAsync(
        GetTenantsQuery query, CancellationToken ct = default)
    {
        var q = db.Tenants.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.ToLower();
            q = q.Where(t => t.Name.ToLower().Contains(s) || t.Slug.ToLower().Contains(s));
        }

        var total = await q.CountAsync(ct);

        var tenants = await q
            .OrderBy(t => t.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var tenantIds = tenants.Select(t => t.Id).ToList();
        var marinaCounts = await db.Marinas
            .Where(m => tenantIds.Contains(m.TenantId))
            .GroupBy(m => m.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, ct);

        var items = tenants.Select(t => new TenantSummaryDto(
            Id: t.Id,
            Name: t.Name,
            Slug: t.Slug,
            SubscriptionTier: t.SubscriptionTier.ToString(),
            IsActive: t.IsActive,
            IsDemo: t.IsDemo,
            SuspendedAt: t.SuspendedAt,
            CreatedAt: t.CreatedAt,
            MarinaCount: marinaCounts.GetValueOrDefault(t.Id, 0)
        )).ToList();

        return new PagedResult<TenantSummaryDto>(items, total, query.Page, query.PageSize);
    }
}

public class SearchUsersQueryHandler(UserManager<ApplicationUser> userManager)
    : IQueryHandler<SearchUsersQuery, PagedResult<UserSummaryDto>>
{
    public async Task<PagedResult<UserSummaryDto>> HandleAsync(
        SearchUsersQuery query, CancellationToken ct = default)
    {
        var q = userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var s = query.Q.ToLower();
            q = q.Where(u =>
                u.Email!.ToLower().Contains(s) ||
                u.FirstName.ToLower().Contains(s) ||
                u.LastName.ToLower().Contains(s) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(s)));
        }

        var total = await q.CountAsync(ct);

        var users = await q
            .OrderBy(u => u.Email)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var items = new List<UserSummaryDto>(users.Count);
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            items.Add(new UserSummaryDto(
                Id: u.Id,
                Email: u.Email!,
                FirstName: u.FirstName,
                LastName: u.LastName,
                IsActive: u.IsActive,
                EmailConfirmed: u.EmailConfirmed,
                CreatedAt: u.CreatedAt,
                LastLoginAt: u.LastLoginAt,
                IsPlatformOperator: roles.Contains("PlatformOperator")
            ));
        }

        return new PagedResult<UserSummaryDto>(items, total, query.Page, query.PageSize);
    }
}

public class GetAuditLogQueryHandler(
    AppDbContext db,
    UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetAuditLogQuery, PagedResult<AuditLogEntryDto>>
{
    public async Task<PagedResult<AuditLogEntryDto>> HandleAsync(
        GetAuditLogQuery query, CancellationToken ct = default)
    {
        var q = db.AuditLogs.AsQueryable();
        if (query.TenantId.HasValue)
            q = q.Where(a => a.TenantId == query.TenantId);

        var total = await q.CountAsync(ct);

        var entries = await q
            .OrderByDescending(a => a.OccurredAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var items = new List<AuditLogEntryDto>(entries.Count);
        foreach (var e in entries)
        {
            string actorName = "";
            if (e.ActorUserId.HasValue)
            {
                var actor = await userManager.FindByIdAsync(e.ActorUserId.Value.ToString());
                actorName = actor is null ? "" : $"{actor.FirstName} {actor.LastName}".Trim();
            }

            items.Add(new AuditLogEntryDto(
                Id: e.Id,
                TenantId: e.TenantId,
                ActorUserId: e.ActorUserId,
                ActorName: actorName,
                Action: e.Action,
                TargetType: e.TargetType,
                TargetId: e.TargetId,
                Details: e.Details,
                OccurredAt: e.OccurredAt
            ));
        }

        return new PagedResult<AuditLogEntryDto>(items, total, query.Page, query.PageSize);
    }
}

public class GetUserProfileQueryHandler(
    AppDbContext db,
    UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetUserProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> HandleAsync(GetUserProfileQuery query, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(query.UserId.ToString())
            ?? throw new KeyNotFoundException("User not found.");

        var roles = await userManager.GetRolesAsync(user);
        var userSummary = new UserSummaryDto(
            Id: user.Id,
            Email: user.Email!,
            FirstName: user.FirstName,
            LastName: user.LastName,
            IsActive: user.IsActive,
            EmailConfirmed: user.EmailConfirmed,
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt,
            IsPlatformOperator: roles.Contains("PlatformOperator")
        );

        var vessels = await db.Vessels
            .Where(v => v.OwnerUserId == query.UserId)
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new VesselSummaryDto(
                Id: v.Id,
                Name: v.Name,
                Make: v.Make,
                Model: v.Model,
                Year: v.Year,
                Length: v.Length,
                Beam: v.Beam,
                Draft: v.Draft,
                BoatType: v.BoatType.ToString(),
                HullColor: v.HullColor,
                RegistrationNumber: v.RegistrationNumber,
                RegistrationState: v.RegistrationState,
                IsArchived: v.IsArchived,
                CreatedAt: v.CreatedAt
            ))
            .ToListAsync(ct);

        var reservations = await db.Reservations
            .Where(r => r.BoaterUserId == query.UserId)
            .OrderByDescending(r => r.RequestedAt)
            .Join(db.Marinas, r => r.Slip.MarinaId, m => m.Id, (r, m) => new { r, m })
            .Select(x => new ReservationSummaryDto(
                Id: x.r.Id,
                VesselId: x.r.VesselId,
                VesselName: x.r.Vessel.Name,
                SlipId: x.r.SlipId,
                SlipName: x.r.Slip.Name,
                MarinaId: x.r.Slip.MarinaId,
                MarinaName: x.m.Name,
                ArrivesAt: x.r.ArrivesAt,
                DepartsAt: x.r.DepartsAt,
                Status: x.r.Status.ToString(),
                BasePrice: x.r.BasePrice,
                Fees: x.r.Fees,
                Taxes: x.r.Taxes,
                Total: x.r.Total,
                RequestedAt: x.r.RequestedAt,
                ConfirmedAt: x.r.ConfirmedAt,
                CancelledAt: x.r.CancelledAt
            ))
            .ToListAsync(ct);

        var memberships = await db.Memberships
            .Where(m => m.UserId == query.UserId)
            .OrderByDescending(m => m.InvitedAt)
            .Select(m => new MembershipSummaryDto(
                Id: m.Id,
                UserId: m.UserId,
                TenantId: m.TenantId,
                MarinaId: m.MarinaId,
                Scope: m.Scope.ToString(),
                Role: m.Role.ToString(),
                InvitedAt: m.InvitedAt,
                AcceptedAt: m.AcceptedAt,
                IsPending: m.AcceptedAt == null
            ))
            .ToListAsync(ct);

        var auditLogs = await db.AuditLogs
            .Where(a => a.TargetType == "User" && a.TargetId == query.UserId.ToString())
            .OrderByDescending(a => a.OccurredAt)
            .Take(20)
            .ToListAsync(ct);

        var actorIds = auditLogs
            .Where(a => a.ActorUserId.HasValue)
            .Select(a => a.ActorUserId!.Value)
            .Distinct()
            .ToList();

        var actorNames = await userManager.Users
            .Where(u => actorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim(), ct);

        var auditEntries = auditLogs.Select(a => new AuditLogEntryDto(
            Id: a.Id,
            TenantId: a.TenantId,
            ActorUserId: a.ActorUserId,
            ActorName: a.ActorUserId.HasValue && actorNames.TryGetValue(a.ActorUserId.Value, out var name) ? name : "",
            Action: a.Action,
            TargetType: a.TargetType,
            TargetId: a.TargetId,
            Details: a.Details,
            OccurredAt: a.OccurredAt
        )).ToList();

        return new UserProfileDto(userSummary, vessels, reservations, memberships, auditEntries);
    }
}

public class GetListingModerationQueueQueryHandler(AppDbContext db)
    : IQueryHandler<GetListingModerationQueueQuery, PagedResult<ListingModerationDto>>
{
    public async Task<PagedResult<ListingModerationDto>> HandleAsync(
        GetListingModerationQueueQuery query, CancellationToken ct = default)
    {
        var q = db.AvailabilityWindows
            .Where(w => w.Status == AvailabilityWindowStatus.Open || w.Status == AvailabilityWindowStatus.Paused)
            .Join(db.Slips, w => w.SlipId, s => s.Id, (w, s) => new { w, s })
            .Join(db.Marinas, x => x.s.MarinaId, m => m.Id, (x, m) => new { x.w, x.s, m })
            .Join(db.Tenants, x => x.m.TenantId, t => t.Id, (x, t) => new { x.w, x.s, x.m, t });

        var total = await q.CountAsync(ct);

        var rows = await q
            .OrderByDescending(x => x.w.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new ListingModerationDto(
                Id: x.w.Id,
                SlipId: x.s.Id,
                SlipName: x.s.Name,
                MarinaId: x.m.Id,
                MarinaName: x.m.Name,
                TenantId: x.t.Id,
                TenantName: x.t.Name,
                Status: x.w.Status.ToString(),
                StartsAt: x.w.StartsAt,
                EndsAt: x.w.EndsAt,
                BasePricePerNight: x.w.BasePricePerNight,
                ListedByKind: x.w.ListedByKind.ToString(),
                CreatedAt: x.w.CreatedAt
            ))
            .ToListAsync(ct);

        return new PagedResult<ListingModerationDto>(rows, total, query.Page, query.PageSize);
    }
}
