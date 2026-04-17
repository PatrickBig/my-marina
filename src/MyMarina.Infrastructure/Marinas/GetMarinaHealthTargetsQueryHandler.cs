using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class GetMarinaHealthTargetsQueryHandler(AppDbContext db) : IQueryHandler<GetMarinaHealthTargetsQuery, HealthTargetsDto?>
{
    public async Task<HealthTargetsDto?> HandleAsync(GetMarinaHealthTargetsQuery query, CancellationToken ct = default)
    {
        var marina = await db.Marinas
            .Select(m => new { m.Id, m.HealthTargets })
            .FirstOrDefaultAsync(m => m.Id == query.MarinaId, ct);

        if (marina is null) return null;

        return new HealthTargetsDto(
            marina.HealthTargets.OccupancyRateTarget,
            marina.HealthTargets.OverdueARThresholdDays,
            marina.HealthTargets.TargetMonthlyRevenue);
    }
}
