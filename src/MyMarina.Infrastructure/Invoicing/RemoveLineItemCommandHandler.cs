using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoicing;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoicing;

internal sealed class RemoveLineItemCommandHandler(AppDbContext db)
    : ICommandHandler<RemoveLineItemCommand>
{
    public async Task HandleAsync(RemoveLineItemCommand command, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == command.InvoiceId && i.MarinaId == command.MarinaId, ct)
            ?? throw new InvalidOperationException("Invoice not found.");

        if (invoice.Status is InvoiceStatus.Voided or InvoiceStatus.Paid)
            throw new InvalidOperationException("Cannot remove line items from a voided or paid invoice.");

        var lineItem = invoice.LineItems.FirstOrDefault(l => l.Id == command.LineItemId)
            ?? throw new InvalidOperationException("Line item not found.");

        db.InvoiceLineItems.Remove(lineItem);

        invoice.SubTotal    = invoice.SubTotal - lineItem.LineTotal;
        invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount;

        await db.SaveChangesAsync(ct);
    }
}
