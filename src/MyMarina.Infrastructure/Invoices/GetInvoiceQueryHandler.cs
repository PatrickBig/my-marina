using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoices;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoices;

public class GetInvoiceQueryHandler(AppDbContext db)
    : IQueryHandler<GetInvoiceQuery, InvoiceDetailDto?>
{
    public async Task<InvoiceDetailDto?> HandleAsync(GetInvoiceQuery query, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.CustomerAccount)
            .Include(i => i.LineItems)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == query.InvoiceId, ct);

        if (invoice is null) return null;

        var lineItems = invoice.LineItems
            .Select(li => new InvoiceLineItemDto(
                li.Id,
                li.Description,
                li.Quantity,
                li.UnitPrice,
                li.LineTotal,
                li.SlipAssignmentId))
            .ToList();

        var payments = invoice.Payments
            .OrderBy(p => p.PaidOn)
            .Select(p => new PaymentDto(
                p.Id,
                p.Amount,
                p.PaidOn,
                p.Method,
                p.ReferenceNumber,
                p.Notes,
                p.RecordedByUserId,
                p.CreatedAt))
            .ToList();

        return new InvoiceDetailDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.CustomerAccountId,
            invoice.CustomerAccount.DisplayName,
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
            lineItems,
            payments);
    }
}
