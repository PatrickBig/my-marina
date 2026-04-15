using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoices;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoices;

public class SendInvoiceCommandHandler(AppDbContext db) : ICommandHandler<SendInvoiceCommand>
{
    public async Task HandleAsync(SendInvoiceCommand command, CancellationToken ct = default)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == command.InvoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice {command.InvoiceId} not found.");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only Draft invoices can be sent.");

        invoice.Status = InvoiceStatus.Sent;
        await db.SaveChangesAsync(ct);
    }
}
