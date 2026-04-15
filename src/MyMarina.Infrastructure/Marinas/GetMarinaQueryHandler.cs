using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Common;
using MyMarina.Application.Marinas;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class GetMarinaQueryHandler(AppDbContext db) : IQueryHandler<GetMarinaQuery, MarinaDto?>
{
    public async Task<MarinaDto?> HandleAsync(GetMarinaQuery query, CancellationToken ct = default)
    {
        var m = await db.Marinas.FirstOrDefaultAsync(x => x.Id == query.MarinaId, ct);
        if (m is null) return null;

        return new MarinaDto(
            m.Id,
            m.TenantId,
            m.Name,
            new AddressDto(m.Address.Street, m.Address.City, m.Address.State, m.Address.Zip, m.Address.Country),
            m.PhoneNumber,
            m.Email,
            m.TimeZoneId,
            m.Website,
            m.Description,
            m.CreatedAt);
    }
}
