using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class SearchMarinasQueryHandler(AppDbContext db)
    : IQueryHandler<SearchMarinasQuery, IReadOnlyList<MarinaSummaryDto>>
{
    public async Task<IReadOnlyList<MarinaSummaryDto>> HandleAsync(SearchMarinasQuery query, CancellationToken ct = default)
    {
        var q = db.Marinas
            .AsNoTracking()
            .Where(m => !m.Tenant.IsDemo);

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim().ToLower();
            q = q.Where(m => m.Name.ToLower().Contains(term)
                           || (m.AddressCity != null && m.AddressCity.ToLower().Contains(term)));
        }

        var limit = Math.Clamp(query.Limit, 1, 50);

        var results = await q
            .OrderBy(m => m.Name)
            .Take(limit)
            .Select(m => new MarinaSummaryDto(
                m.Id,
                m.Name,
                m.AddressCity,
                m.AddressState,
                m.MarinaType.ToString()))
            .ToListAsync(ct);

        return results;
    }
}
