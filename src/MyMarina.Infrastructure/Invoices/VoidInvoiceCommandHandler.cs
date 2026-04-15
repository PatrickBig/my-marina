using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoices;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoices;

public class VoidInvoiceCommandHandler(AppDbContext db) : ICommandHandler<VoidInvoiceCommand>
{
    public async Task HandleAsync(VoidInvoiceCommand command, CancellationToken ct = default)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == command.InvoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice {command.InvoiceId} not found.");

        if (invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Paid invoices cannot be voided.");

        if (invoice.Status == InvoiceStatus.Voided)
            throw new InvalidOperationException("Invoice is already voided.");

        invoice.Status = InvoiceStatus.Voided;
        await db.SaveChangesAsync(ct);
    }
}
