using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoicing;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoicing;

internal sealed class SendInvoiceCommandHandler(AppDbContext db, IEmailService emailService)
    : ICommandHandler<SendInvoiceCommand>
{
    public async Task HandleAsync(SendInvoiceCommand command, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.BillingAccount)
            .Include(i => i.Marina)
            .FirstOrDefaultAsync(i => i.Id == command.InvoiceId && i.MarinaId == command.MarinaId, ct)
            ?? throw new InvalidOperationException("Invoice not found.");

        if (invoice.Status is InvoiceStatus.Voided)
            throw new InvalidOperationException("Cannot send a voided invoice.");

        if (invoice.Status is InvoiceStatus.Paid)
            throw new InvalidOperationException("Cannot send a paid invoice.");

        invoice.Status = InvoiceStatus.Sent;
        await db.SaveChangesAsync(ct);

        await emailService.SendInvoiceSentAsync(
            toEmail: invoice.BillingAccount.BillingEmail,
            marinaName: invoice.Marina.Name,
            billingAccountName: invoice.BillingAccount.DisplayName,
            invoiceNumber: invoice.InvoiceNumber,
            totalAmount: invoice.TotalAmount,
            dueDate: invoice.DueDate,
            invoiceId: invoice.Id,
            ct);
    }
}
