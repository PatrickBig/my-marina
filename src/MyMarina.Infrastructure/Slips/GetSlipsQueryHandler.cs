using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Slips;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Slips;

public class GetSlipsQueryHandler(AppDbContext db) : IQueryHandler<GetSlipsQuery, IReadOnlyList<SlipDto>>
{
    public async Task<IReadOnlyList<SlipDto>> HandleAsync(GetSlipsQuery query, CancellationToken ct = default)
    {
        return await db.Slips
            .Where(s => s.MarinaId == query.MarinaId)
            .OrderBy(s => s.Name)
            .Select(s => new SlipDto(
                s.Id, s.MarinaId, s.DockId, s.Name, s.SlipType,
                s.MaxLength, s.MaxBeam, s.MaxDraft,
                s.HasElectric, s.Electric, s.HasWater,
                s.RateType, s.DailyRate, s.MonthlyRate, s.AnnualRate,
                s.Status, s.Latitude, s.Longitude, s.Notes, s.CreatedAt))
            .ToListAsync(ct);
    }
}
