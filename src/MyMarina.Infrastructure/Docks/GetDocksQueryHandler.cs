using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Docks;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Docks;

public class GetDocksQueryHandler(AppDbContext db) : IQueryHandler<GetDocksQuery, IReadOnlyList<DockDto>>
{
    public async Task<IReadOnlyList<DockDto>> HandleAsync(GetDocksQuery query, CancellationToken ct = default)
    {
        return await db.Docks
            .Where(d => d.MarinaId == query.MarinaId)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .Select(d => new DockDto(d.Id, d.MarinaId, d.Name, d.Description, d.SortOrder, d.CreatedAt))
            .ToListAsync(ct);
    }
}
