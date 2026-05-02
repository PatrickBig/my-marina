using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.VesselRecords;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.VesselRecords;

public class GetVesselRecordsQueryHandler(AppDbContext db)
    : IQueryHandler<GetVesselRecordsQuery, IReadOnlyList<VesselRecordDto>>
{
    public async Task<IReadOnlyList<VesselRecordDto>> HandleAsync(GetVesselRecordsQuery query, CancellationToken ct = default)
    {
        var q = db.MarinaVesselRecords
            .Include(r => r.Vessel)
            .Where(r => r.MarinaId == query.MarinaId);

        if (query.BillingAccountId.HasValue)
            q = q.Where(r => r.BillingAccountId == query.BillingAccountId.Value);

        return await q
            .OrderBy(r => r.Vessel.Name)
            .Select(r => CreateVesselRecordCommandHandler.ToDto(r, r.Vessel, r.Vessel.OwnerUserId == null))
            .ToListAsync(ct);
    }
}
