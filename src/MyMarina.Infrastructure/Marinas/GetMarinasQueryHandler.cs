using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Common;
using MyMarina.Application.Marinas;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class GetMarinasQueryHandler(AppDbContext db) : IQueryHandler<GetMarinasQuery, IReadOnlyList<MarinaDto>>
{
    public async Task<IReadOnlyList<MarinaDto>> HandleAsync(GetMarinasQuery query, CancellationToken ct = default)
    {
        return await db.Marinas
            .OrderBy(m => m.Name)
            .Select(m => new MarinaDto(
                m.Id,
                m.TenantId,
                m.Name,
                new AddressDto(m.Address.Street, m.Address.City, m.Address.State, m.Address.Zip, m.Address.Country),
                m.PhoneNumber,
                m.Email,
                m.TimeZoneId,
                m.Website,
                m.Description,
                m.CreatedAt))
            .ToListAsync(ct);
    }
}
