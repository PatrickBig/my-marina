using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.BillingAccounts;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.BillingAccounts;

public class GetBillingAccountQueryHandler(AppDbContext db)
    : IQueryHandler<GetBillingAccountQuery, BillingAccountDto>
{
    public async Task<BillingAccountDto> HandleAsync(GetBillingAccountQuery query, CancellationToken ct = default)
    {
        var account = await db.BillingAccounts
            .FirstOrDefaultAsync(a => a.Id == query.Id && a.MarinaId == query.MarinaId, ct)
            ?? throw new KeyNotFoundException($"BillingAccount {query.Id} not found.");

        return CreateBillingAccountCommandHandler.ToDto(account);
    }
}
