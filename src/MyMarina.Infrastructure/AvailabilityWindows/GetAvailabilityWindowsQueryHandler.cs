using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.AvailabilityWindows;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.AvailabilityWindows;

public class GetAvailabilityWindowsQueryHandler(AppDbContext db)
    : IQueryHandler<GetAvailabilityWindowsQuery, IReadOnlyList<AvailabilityWindowDto>>
{
    public async Task<IReadOnlyList<AvailabilityWindowDto>> HandleAsync(GetAvailabilityWindowsQuery query, CancellationToken ct = default)
    {
        var q = db.AvailabilityWindows
            .Include(w => w.Slip)
            .Where(w => w.Slip.MarinaId == query.MarinaId)
            .AsQueryable();

        if (query.SlipId.HasValue)
            q = q.Where(w => w.SlipId == query.SlipId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (Enum.TryParse<Domain.Enums.AvailabilityWindowStatus>(query.Status, ignoreCase: true, out var status))
                q = q.Where(w => w.Status == status);
        }

        var windows = await q
            .OrderBy(w => w.StartsAt)
            .ToListAsync(ct);

        return windows.Select(AvailabilityWindowHelper.ToDto).ToList();
    }
}
