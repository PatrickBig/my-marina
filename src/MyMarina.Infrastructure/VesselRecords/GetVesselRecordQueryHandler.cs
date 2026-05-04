using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.VesselRecords;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.VesselRecords;

public class GetVesselRecordQueryHandler(AppDbContext db)
    : IQueryHandler<GetVesselRecordQuery, VesselRecordDto>
{
    public async Task<VesselRecordDto> HandleAsync(GetVesselRecordQuery query, CancellationToken ct = default)
    {
        var record = await db.MarinaVesselRecords
            .Include(r => r.Vessel)
            .FirstOrDefaultAsync(r => r.Id == query.Id && r.MarinaId == query.MarinaId, ct)
            ?? throw new KeyNotFoundException($"VesselRecord {query.Id} not found.");

        return CreateVesselRecordCommandHandler.ToDto(record, record.Vessel, record.Vessel.OwnerUserId == null);
    }
}
