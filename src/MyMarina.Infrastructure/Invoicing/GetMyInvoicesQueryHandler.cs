using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoicing;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoicing;

internal sealed class GetMyInvoicesQueryHandler(AppDbContext db)
    : IQueryHandler<GetMyInvoicesQuery, IReadOnlyList<InvoiceSummaryDto>>
{
    public async Task<IReadOnlyList<InvoiceSummaryDto>> HandleAsync(GetMyInvoicesQuery query, CancellationToken ct = default)
    {
        if (query.BillingAccountIds.Count == 0)
            return [];

        var invoices = await db.Invoices
            .Include(i => i.BillingAccount)
            .Where(i => query.BillingAccountIds.Contains(i.BillingAccountId))
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        return invoices
            .Select(i => InvoiceHelper.ToSummaryDto(i, i.BillingAccount.DisplayName))
            .ToList();
    }
}
