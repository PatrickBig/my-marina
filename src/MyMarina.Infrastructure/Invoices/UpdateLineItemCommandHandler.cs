using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoices;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoices;

public class UpdateLineItemCommandHandler(AppDbContext db) : ICommandHandler<UpdateLineItemCommand>
{
    public async Task HandleAsync(UpdateLineItemCommand command, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == command.InvoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice {command.InvoiceId} not found.");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Line items can only be edited on Draft invoices.");

        var lineItem = invoice.LineItems.FirstOrDefault(li => li.Id == command.LineItemId)
            ?? throw new KeyNotFoundException($"Line item {command.LineItemId} not found.");

        lineItem.Description = command.Description;
        lineItem.Quantity    = command.Quantity;
        lineItem.UnitPrice   = command.UnitPrice;
        lineItem.LineTotal   = Math.Round(command.Quantity * command.UnitPrice, 2);

        invoice.SubTotal    = invoice.LineItems.Sum(li => li.LineTotal);
        invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount;

        await db.SaveChangesAsync(ct);
    }
}
