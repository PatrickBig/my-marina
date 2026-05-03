using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoicing;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoicing;

internal sealed class VoidInvoiceCommandHandler(AppDbContext db)
    : ICommandHandler<VoidInvoiceCommand>
{
    public async Task HandleAsync(VoidInvoiceCommand command, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .FirstOrDefaultAsync(i => i.Id == command.InvoiceId && i.MarinaId == command.MarinaId, ct)
            ?? throw new InvalidOperationException("Invoice not found.");

        if (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.PartiallyPaid)
            throw new InvalidOperationException("Cannot void an invoice with recorded payments. Refund payments first.");

        invoice.Status = InvoiceStatus.Voided;
        await db.SaveChangesAsync(ct);
    }
}
