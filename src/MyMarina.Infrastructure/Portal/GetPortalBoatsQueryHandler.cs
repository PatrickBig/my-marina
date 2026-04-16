using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Portal;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Portal;

public class GetPortalBoatsQueryHandler(
    AppDbContext db,
    ICustomerContext customerContext) : IQueryHandler<GetPortalBoatsQuery, IReadOnlyList<PortalBoatDto>>
{
    public async Task<IReadOnlyList<PortalBoatDto>> HandleAsync(GetPortalBoatsQuery query, CancellationToken ct = default)
    {
        return await db.Boats
            .Where(b => b.CustomerAccountId == customerContext.CustomerAccountId)
            .OrderBy(b => b.Name)
            .Select(b => new PortalBoatDto(
                b.Id,
                b.Name,
                b.Make,
                b.Model,
                b.Year,
                b.Length,
                b.Beam,
                b.Draft,
                b.BoatType,
                b.RegistrationNumber,
                b.InsuranceExpiresOn))
            .ToListAsync(ct);
    }
}
