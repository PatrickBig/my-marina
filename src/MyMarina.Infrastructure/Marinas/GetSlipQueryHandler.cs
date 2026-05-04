using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class GetSlipQueryHandler(AppDbContext db)
    : IQueryHandler<GetSlipQuery, SlipDto>
{
    public async Task<SlipDto> HandleAsync(GetSlipQuery query, CancellationToken ct = default)
    {
        var slip = await db.Slips
            .FirstOrDefaultAsync(s => s.Id == query.SlipId && s.MarinaId == query.MarinaId, ct)
            ?? throw new KeyNotFoundException("Slip not found.");
        return MarinaMappers.ToSlipDto(slip);
    }
}
