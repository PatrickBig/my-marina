using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Vessels;

namespace MyMarina.Infrastructure.Vessels;

public class GetVesselQueryHandler(Infrastructure.Persistence.AppDbContext db)
    : IQueryHandler<GetVesselQuery, VesselDto>
{
    public async Task<VesselDto> HandleAsync(GetVesselQuery query, CancellationToken ct = default)
    {
        var vessel = await db.Vessels
            .FirstOrDefaultAsync(v => v.Id == query.Id && v.OwnerUserId == query.OwnerId, ct)
            ?? throw new KeyNotFoundException("Vessel not found.");

        return CreateVesselCommandHandler.ToDto(vessel);
    }
}
