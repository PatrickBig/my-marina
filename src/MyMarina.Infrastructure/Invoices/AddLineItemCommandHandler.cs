using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoices;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoices;

public class AddLineItemCommandHandler(AppDbContext db) : ICommandHandler<AddLineItemCommand, Guid>
{
    public async Task<Guid> HandleAsync(AddLineItemCommand command, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == command.InvoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice {command.InvoiceId} not found.");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Line items can only be added to Draft invoices.");

        var lineItem = new InvoiceLineItem
        {
            TenantId         = invoice.TenantId,
            InvoiceId        = invoice.Id,
            Description      = command.Description,
            Quantity         = command.Quantity,
            UnitPrice        = command.UnitPrice,
            LineTotal        = Math.Round(command.Quantity * command.UnitPrice, 2),
            SlipAssignmentId = command.SlipAssignmentId,
        };

        invoice.LineItems.Add(lineItem);
        RecalculateTotals(invoice);
        await db.SaveChangesAsync(ct);

        return lineItem.Id;
    }

    private static void RecalculateTotals(Invoice invoice)
    {
        invoice.SubTotal    = invoice.LineItems.Sum(li => li.LineTotal);
        invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount;
    }
}
