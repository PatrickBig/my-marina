using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoicing;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoicing;

internal sealed class GetInvoiceQueryHandler(AppDbContext db)
    : IQueryHandler<GetInvoiceQuery, InvoiceDto>
{
    public async Task<InvoiceDto> HandleAsync(GetInvoiceQuery query, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.Marina)
            .Include(i => i.BillingAccount)
            .Include(i => i.LineItems)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == query.InvoiceId && i.MarinaId == query.MarinaId, ct)
            ?? throw new InvalidOperationException("Invoice not found.");

        return InvoiceHelper.ToDto(invoice, invoice.Marina.Name, invoice.BillingAccount.DisplayName);
    }
}
