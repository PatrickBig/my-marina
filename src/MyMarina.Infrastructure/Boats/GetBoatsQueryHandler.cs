using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Boats;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Boats;

public class GetBoatsQueryHandler(AppDbContext db) : IQueryHandler<GetBoatsQuery, IReadOnlyList<BoatDto>>
{
    public async Task<IReadOnlyList<BoatDto>> HandleAsync(GetBoatsQuery query, CancellationToken ct = default)
    {
        return await db.Boats
            .Where(b => b.CustomerAccountId == query.CustomerAccountId)
            .OrderBy(b => b.Name)
            .Select(b => new BoatDto(
                b.Id, b.CustomerAccountId, b.Name, b.Make, b.Model, b.Year,
                b.Length, b.Beam, b.Draft, b.BoatType, b.HullColor,
                b.RegistrationNumber, b.RegistrationState,
                b.InsuranceProvider, b.InsurancePolicyNumber, b.InsuranceExpiresOn,
                b.CreatedAt))
            .ToListAsync(ct);
    }
}
