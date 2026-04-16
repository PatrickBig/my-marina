using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Maintenance;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Maintenance;

public class GetMaintenanceRequestQueryHandler(AppDbContext db)
    : IQueryHandler<GetMaintenanceRequestQuery, MaintenanceRequestDto?>
{
    public async Task<MaintenanceRequestDto?> HandleAsync(
        GetMaintenanceRequestQuery query, CancellationToken ct = default)
    {
        return await db.MaintenanceRequests
            .Include(r => r.CustomerAccount)
            .Where(r => r.Id == query.MaintenanceRequestId)
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
            .FirstOrDefaultAsync(ct);
    }
}
