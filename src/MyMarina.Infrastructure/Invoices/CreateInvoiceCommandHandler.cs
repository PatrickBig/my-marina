using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoices;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoices;

public class CreateInvoiceCommandHandler(
    AppDbContext db,
    ITenantContext tenantContext) : ICommandHandler<CreateInvoiceCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateInvoiceCommand command, CancellationToken ct = default)
    {
        var customerExists = await db.CustomerAccounts.AnyAsync(
            c => c.Id == command.CustomerAccountId, ct);

        if (!customerExists)
            throw new KeyNotFoundException($"Customer account {command.CustomerAccountId} not found.");

        var invoiceNumber = await GenerateInvoiceNumberAsync(ct);

        var invoice = new Invoice
        {
            TenantId         = tenantContext.TenantId,
            CustomerAccountId = command.CustomerAccountId,
            InvoiceNumber    = invoiceNumber,
            IssuedDate       = command.IssuedDate,
            DueDate          = command.DueDate,
            Notes            = command.Notes,
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);

        return invoice.Id;
    }

    private async Task<string> GenerateInvoiceNumberAsync(CancellationToken ct)
    {
        var count = await db.Invoices.CountAsync(ct);
        return $"INV-{count + 1:D5}";
    }
}
