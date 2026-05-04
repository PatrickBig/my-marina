using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.AvailabilityWindows;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.AvailabilityWindows;

public class GetAvailabilityWindowQueryHandler(AppDbContext db)
    : IQueryHandler<GetAvailabilityWindowQuery, AvailabilityWindowDto>
{
    public async Task<AvailabilityWindowDto> HandleAsync(GetAvailabilityWindowQuery query, CancellationToken ct = default)
    {
        var window = await db.AvailabilityWindows
            .Include(w => w.Slip)
            .FirstOrDefaultAsync(w => w.Id == query.Id && w.Slip.MarinaId == query.MarinaId, ct)
            ?? throw new KeyNotFoundException($"AvailabilityWindow {query.Id} not found.");

        return AvailabilityWindowHelper.ToDto(window);
    }
}
