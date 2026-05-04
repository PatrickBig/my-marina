using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class GetSlipsQueryHandler(AppDbContext db)
    : IQueryHandler<GetSlipsQuery, IReadOnlyList<SlipDto>>
{
    public async Task<IReadOnlyList<SlipDto>> HandleAsync(GetSlipsQuery query, CancellationToken ct = default)
    {
        var q = db.Slips.Where(s => s.MarinaId == query.MarinaId);
        if (query.DockId.HasValue)
            q = q.Where(s => s.DockId == query.DockId);
        var slips = await q.OrderBy(s => s.Name).ToListAsync(ct);
        return slips.Select(MarinaMappers.ToSlipDto).ToList();
    }
}
