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
                u.LastName.ToLower().Contains(s));
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
