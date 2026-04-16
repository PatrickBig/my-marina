using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Portal;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Portal;

public class GetPortalInvoiceQueryHandler(
    AppDbContext db,
    ICustomerContext customerContext) : IQueryHandler<GetPortalInvoiceQuery, PortalInvoiceDetailDto?>
{
    public async Task<PortalInvoiceDetailDto?> HandleAsync(GetPortalInvoiceQuery query, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Payments)
            .Where(i => i.Id == query.InvoiceId
                     && i.CustomerAccountId == customerContext.CustomerAccountId)
            .FirstOrDefaultAsync(ct);

        if (invoice is null) return null;

        return new PortalInvoiceDetailDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.Status,
            invoice.IssuedDate,
            invoice.DueDate,
            invoice.SubTotal,
            invoice.TaxAmount,
            invoice.TotalAmount,
            invoice.AmountPaid,
            invoice.BalanceDue,
            invoice.Notes,
            invoice.CreatedAt,
            invoice.LineItems.Select(li => new PortalInvoiceLineItemDto(
                li.Description, li.Quantity, li.UnitPrice, li.LineTotal)).ToList(),
            invoice.Payments.OrderBy(p => p.PaidOn)
                .Select(p => new PortalPaymentDto(
                    p.Amount, p.PaidOn, p.Method, p.ReferenceNumber)).ToList());
    }
}
