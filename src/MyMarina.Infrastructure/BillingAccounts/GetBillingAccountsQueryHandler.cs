using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.BillingAccounts;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.BillingAccounts;

public class GetBillingAccountsQueryHandler(AppDbContext db)
    : IQueryHandler<GetBillingAccountsQuery, IReadOnlyList<BillingAccountDto>>
{
    public async Task<IReadOnlyList<BillingAccountDto>> HandleAsync(GetBillingAccountsQuery query, CancellationToken ct = default)
    {
        return await db.BillingAccounts
            .Where(a => a.MarinaId == query.MarinaId)
            .OrderBy(a => a.DisplayName)
            .Select(a => CreateBillingAccountCommandHandler.ToDto(a))
            .ToListAsync(ct);
    }
}
