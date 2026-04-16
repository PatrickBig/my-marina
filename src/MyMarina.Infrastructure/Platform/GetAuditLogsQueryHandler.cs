using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Platform;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Platform;

public class GetAuditLogsQueryHandler(AppDbContext db)
    : IQueryHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    public async Task<PagedResult<AuditLogDto>> HandleAsync(
        GetAuditLogsQuery query, CancellationToken ct = default)
    {
        var q = db.AuditLogs.AsQueryable();

        if (query.TenantId.HasValue)
            q = q.Where(l => l.TenantId == query.TenantId.Value);

        if (query.UserId.HasValue)
            q = q.Where(l => l.UserId == query.UserId.Value);

        if (!string.IsNullOrWhiteSpace(query.Action))
            q = q.Where(l => l.Action.Contains(query.Action));

        if (!string.IsNullOrWhiteSpace(query.EntityType))
            q = q.Where(l => l.EntityType == query.EntityType);

        if (query.From.HasValue)
            q = q.Where(l => l.Timestamp >= query.From.Value);

        if (query.To.HasValue)
            q = q.Where(l => l.Timestamp <= query.To.Value);

        var totalCount = await q.CountAsync(ct);
        var pageSize = Math.Min(query.PageSize, 100);
        var skip = (query.Page - 1) * pageSize;

        var items = await q
            .OrderByDescending(l => l.Timestamp)
            .Skip(skip)
            .Take(pageSize)
            .Select(l => new AuditLogDto(
                l.Id,
                l.TenantId,
                l.TenantId != null
                    ? db.Tenants.Where(t => t.Id == l.TenantId).Select(t => t.Name).FirstOrDefault()
                    : null,
                l.UserId,
                db.Users.Where(u => u.Id == l.UserId).Select(u => u.Email!).FirstOrDefault() ?? "(unknown)",
                l.Action,
                l.EntityType,
                l.EntityId,
                l.Before,
                l.After,
                l.IpAddress,
                l.Timestamp))
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>(items, totalCount, query.Page, pageSize);
    }
}
