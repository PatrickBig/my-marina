using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoicing;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoicing;

internal sealed class AddLineItemCommandHandler(AppDbContext db)
    : ICommandHandler<AddLineItemCommand, InvoiceLineItemDto>
{
    public async Task<InvoiceLineItemDto> HandleAsync(AddLineItemCommand command, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .FirstOrDefaultAsync(i => i.Id == command.InvoiceId && i.MarinaId == command.MarinaId, ct)
            ?? throw new InvalidOperationException("Invoice not found.");

        if (invoice.Status is InvoiceStatus.Voided or InvoiceStatus.Paid)
            throw new InvalidOperationException("Cannot add line items to a voided or paid invoice.");

        var lineItem = new InvoiceLineItem
        {
            InvoiceId        = command.InvoiceId,
            Description      = command.Description,
            Quantity         = command.Quantity,
            UnitPrice        = command.UnitPrice,
            LineTotal        = Math.Round(command.Quantity * command.UnitPrice, 2),
            SlipAssignmentId = command.SlipAssignmentId,
            ReservationId    = command.ReservationId,
        };

        db.InvoiceLineItems.Add(lineItem);

        invoice.SubTotal    = invoice.SubTotal + lineItem.LineTotal;
        invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount;

        await db.SaveChangesAsync(ct);

        return InvoiceHelper.ToLineItemDto(lineItem);
    }
}
