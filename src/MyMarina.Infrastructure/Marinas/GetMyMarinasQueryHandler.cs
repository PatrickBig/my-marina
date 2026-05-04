using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class GetMyMarinasQueryHandler(AppDbContext db, IUserContext userContext)
    : IQueryHandler<GetMyMarinasQuery, IReadOnlyList<MyMarinaDto>>
{
    public async Task<IReadOnlyList<MyMarinaDto>> HandleAsync(GetMyMarinasQuery query, CancellationToken ct = default)
    {
        var result = new Dictionary<Guid, MyMarinaDto>();

        // Collect marina IDs from staff memberships
        var staffMarinaIds = userContext.Memberships
            .Where(m => m.MarinaId.HasValue)
            .Select(m => (MarinaId: m.MarinaId!.Value, Role: m.Role.ToString()))
            .ToList();

        // Collect marina IDs from billing account memberships (boater's marina)
        var billingMarinaIds = userContext.BillingAccounts
            .Select(b => (MarinaId: b.MarinaId, Role: b.Role.ToString()))
            .ToList();

        var allMarinaIds = staffMarinaIds.Select(x => x.MarinaId)
            .Union(billingMarinaIds.Select(x => x.MarinaId))
            .Distinct()
            .ToList();

        if (allMarinaIds.Count == 0)
            return [];

        var marinas = await db.Marinas
            .IgnoreQueryFilters()
            .Where(m => allMarinaIds.Contains(m.Id))
            .ToListAsync(ct);

        foreach (var m in marinas)
        {
            // Staff membership takes priority over billing account
            var staffEntry = staffMarinaIds.FirstOrDefault(x => x.MarinaId == m.Id);
            if (staffEntry != default)
            {
                result[m.Id] = new MyMarinaDto(
                    Id: m.Id,
                    TenantId: m.TenantId,
                    Name: m.Name,
                    AddressCity: m.AddressCity,
                    AddressState: m.AddressState,
                    MarinaType: m.MarinaType.ToString(),
                    IsListed: m.IsListed,
                    Latitude: m.Latitude,
                    Longitude: m.Longitude,
                    UserRole: staffEntry.Role,
                    RelationshipKind: "Staff"
                );
            }
            else
            {
                var billingEntry = billingMarinaIds.FirstOrDefault(x => x.MarinaId == m.Id);
                result[m.Id] = new MyMarinaDto(
                    Id: m.Id,
                    TenantId: m.TenantId,
                    Name: m.Name,
                    AddressCity: m.AddressCity,
                    AddressState: m.AddressState,
                    MarinaType: m.MarinaType.ToString(),
                    IsListed: m.IsListed,
                    Latitude: m.Latitude,
                    Longitude: m.Longitude,
                    UserRole: billingEntry.Role,
                    RelationshipKind: "BillingAccount"
                );
            }
        }

        return [.. result.Values.OrderBy(m => m.Name)];
    }
}
