using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Maintenance;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Maintenance;

public class GetMaintenanceRequestsQueryHandler(AppDbContext db)
    : IQueryHandler<GetMaintenanceRequestsQuery, IReadOnlyList<MaintenanceRequestDto>>
{
    public async Task<IReadOnlyList<MaintenanceRequestDto>> HandleAsync(
        GetMaintenanceRequestsQuery query, CancellationToken ct = default)
    {
        var q = db.MaintenanceRequests
            .Include(r => r.CustomerAccount)
            .AsQueryable();

        if (query.Status.HasValue)
            q = q.Where(r => r.Status == query.Status.Value);

        if (query.Priority.HasValue)
            q = q.Where(r => r.Priority == query.Priority.Value);

        var items = await q
            .OrderByDescending(r => r.SubmittedAt)
            .Select(r => new MaintenanceRequestDto(
                r.Id,
                r.CustomerAccountId,
                r.CustomerAccount.DisplayName,
                r.SlipId,
                r.SlipId != null
                    ? db.Slips.Where(s => s.Id == r.SlipId).Select(s => s.Name).FirstOrDefault()
                    : null,
                r.BoatId,
                r.BoatId != null
                    ? db.Boats.Where(b => b.Id == r.BoatId).Select(b => b.Name).FirstOrDefault()
                    : null,
                r.Title,
                r.Description,
                r.Status,
                r.Priority,
                r.SubmittedAt,
                r.ResolvedAt,
                r.WorkOrder != null ? r.WorkOrder.Id : (Guid?)null))
            .ToListAsync(ct);

        return items;
    }
}
