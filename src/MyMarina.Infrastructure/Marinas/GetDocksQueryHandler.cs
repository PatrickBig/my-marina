using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class GetDocksQueryHandler(AppDbContext db)
    : IQueryHandler<GetDocksQuery, IReadOnlyList<DockDto>>
{
    public async Task<IReadOnlyList<DockDto>> HandleAsync(GetDocksQuery query, CancellationToken ct = default)
    {
        var docks = await db.Docks
            .Where(d => d.MarinaId == query.MarinaId)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .ToListAsync(ct);
        return docks.Select(MarinaMappers.ToDockDto).ToList();
    }
}
