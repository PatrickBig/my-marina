using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Portal;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Portal;

public class GetPortalMaintenanceRequestsQueryHandler(
    AppDbContext db,
    ICustomerContext customerContext) : IQueryHandler<GetPortalMaintenanceRequestsQuery, IReadOnlyList<PortalMaintenanceRequestDto>>
{
    public async Task<IReadOnlyList<PortalMaintenanceRequestDto>> HandleAsync(
        GetPortalMaintenanceRequestsQuery query, CancellationToken ct = default)
    {
        return await db.MaintenanceRequests
            .Where(mr => mr.CustomerAccountId == customerContext.CustomerAccountId)
            .OrderByDescending(mr => mr.SubmittedAt)
            .Select(mr => new PortalMaintenanceRequestDto(
                mr.Id,
                mr.Title,
                mr.Description,
                mr.Status,
                mr.Priority,
                mr.SlipId,
                mr.SlipId != null
                    ? db.Slips.Where(s => s.Id == mr.SlipId).Select(s => s.Name).FirstOrDefault()
                    : null,
                mr.BoatId,
                mr.BoatId != null
                    ? db.Boats.Where(b => b.Id == mr.BoatId).Select(b => b.Name).FirstOrDefault()
                    : null,
                mr.SubmittedAt,
                mr.ResolvedAt))
            .ToListAsync(ct);
    }
}
