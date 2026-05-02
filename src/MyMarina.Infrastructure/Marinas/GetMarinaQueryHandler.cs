using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class GetMarinaQueryHandler(AppDbContext db)
    : IQueryHandler<GetMarinaQuery, MarinaDto>
{
    public async Task<MarinaDto> HandleAsync(GetMarinaQuery query, CancellationToken ct = default)
    {
        var marina = await db.Marinas
            .FirstOrDefaultAsync(m => m.Id == query.MarinaId, ct)
            ?? throw new KeyNotFoundException("Marina not found.");
        return MarinaMappers.ToMarinaDto(marina);
    }
}
