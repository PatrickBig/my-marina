using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoicing;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoicing;

internal sealed class GetInvoicesQueryHandler(AppDbContext db)
    : IQueryHandler<GetInvoicesQuery, IReadOnlyList<InvoiceSummaryDto>>
{
    public async Task<IReadOnlyList<InvoiceSummaryDto>> HandleAsync(GetInvoicesQuery query, CancellationToken ct = default)
    {
        var q = db.Invoices
            .Include(i => i.BillingAccount)
            .Where(i => i.MarinaId == query.MarinaId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<InvoiceStatus>(query.Status, ignoreCase: true, out var status))
        {
            q = q.Where(i => i.Status == status);
        }

        if (query.BillingAccountId.HasValue)
            q = q.Where(i => i.BillingAccountId == query.BillingAccountId.Value);

        var invoices = await q
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        return invoices
            .Select(i => InvoiceHelper.ToSummaryDto(i, i.BillingAccount.DisplayName))
            .ToList();
    }
}
