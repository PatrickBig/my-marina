using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoices;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoices;

public class UpdateInvoiceDraftCommandHandler(AppDbContext db) : ICommandHandler<UpdateInvoiceDraftCommand>
{
    public async Task HandleAsync(UpdateInvoiceDraftCommand command, CancellationToken ct = default)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == command.InvoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice {command.InvoiceId} not found.");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only Draft invoices can be edited.");

        invoice.IssuedDate = command.IssuedDate;
        invoice.DueDate    = command.DueDate;
        invoice.Notes      = command.Notes;

        await db.SaveChangesAsync(ct);
    }
}
