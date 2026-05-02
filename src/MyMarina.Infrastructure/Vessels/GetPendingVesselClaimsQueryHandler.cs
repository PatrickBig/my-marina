using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Vessels;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Vessels;

public class GetPendingVesselClaimsQueryHandler(AppDbContext db)
    : IQueryHandler<GetPendingVesselClaimsQuery, IReadOnlyList<PendingVesselClaimDto>>
{
    public async Task<IReadOnlyList<PendingVesselClaimDto>> HandleAsync(GetPendingVesselClaimsQuery query, CancellationToken ct = default)
    {
        var normalizedEmail = query.UserEmail.Trim().ToLowerInvariant();

        // Find all unclaimed vessels where ClaimEmail matches the user's email,
        // joined with the marina that created each vessel record.
        return await db.MarinaVesselRecords
            .Include(r => r.Vessel)
            .Include(r => r.Marina)
            .Where(r => r.Vessel.OwnerUserId == null
                     && r.Vessel.ClaimEmail == normalizedEmail)
            .Select(r => new PendingVesselClaimDto(
                r.VesselId,
                r.Vessel.Name,
                r.Vessel.Make,
                r.Vessel.Model,
                r.Vessel.Year,
                r.Vessel.Length,
                r.Vessel.BoatType.ToString(),
                r.MarinaId,
                r.Marina.Name))
            .Distinct()
            .ToListAsync(ct);
    }
}
