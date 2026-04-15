using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoices;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoices;

public class RemoveLineItemCommandHandler(AppDbContext db) : ICommandHandler<RemoveLineItemCommand>
{
    public async Task HandleAsync(RemoveLineItemCommand command, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == command.InvoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice {command.InvoiceId} not found.");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Line items can only be removed from Draft invoices.");

        var lineItem = invoice.LineItems.FirstOrDefault(li => li.Id == command.LineItemId)
            ?? throw new KeyNotFoundException($"Line item {command.LineItemId} not found.");

        invoice.LineItems.Remove(lineItem);
        db.InvoiceLineItems.Remove(lineItem);

        invoice.SubTotal    = invoice.LineItems.Sum(li => li.LineTotal);
        invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount;

        await db.SaveChangesAsync(ct);
    }
}
