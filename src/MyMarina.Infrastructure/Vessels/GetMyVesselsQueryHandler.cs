using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Vessels;

namespace MyMarina.Infrastructure.Vessels;

public class GetMyVesselsQueryHandler(Infrastructure.Persistence.AppDbContext db)
    : IQueryHandler<GetMyVesselsQuery, IReadOnlyList<VesselDto>>
{
    public async Task<IReadOnlyList<VesselDto>> HandleAsync(
        GetMyVesselsQuery query,
        CancellationToken ct = default)
    {
        var vessels = await db.Vessels
            .Where(v => v.OwnerUserId == query.OwnerId && !v.IsArchived)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(ct);

        return vessels.Select(CreateVesselCommandHandler.ToDto).ToList();
    }
}
