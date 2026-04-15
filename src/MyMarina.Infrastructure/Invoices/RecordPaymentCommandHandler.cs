using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoices;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoices;

public class RecordPaymentCommandHandler(
    AppDbContext db,
    ITenantContext tenantContext) : ICommandHandler<RecordPaymentCommand, Guid>
{
    public async Task<Guid> HandleAsync(RecordPaymentCommand command, CancellationToken ct = default)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == command.InvoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice {command.InvoiceId} not found.");

        if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Voided)
            throw new InvalidOperationException("Payments can only be recorded against Sent, Overdue, or PartiallyPaid invoices.");

        if (invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Invoice is already fully paid.");

        if (command.Amount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero.");

        if (command.Amount > invoice.BalanceDue)
            throw new InvalidOperationException($"Payment amount ({command.Amount:C}) exceeds balance due ({invoice.BalanceDue:C}).");

        var payment = new Payment
        {
            TenantId                  = tenantContext.TenantId,
            InvoiceId                 = invoice.Id,
            Amount                    = command.Amount,
            PaidOn                    = command.PaidOn,
            Method                    = command.Method,
            ReferenceNumber           = command.ReferenceNumber,
            Notes                     = command.Notes,
            RecordedByUserId          = command.RecordedByUserId,
        };

        db.Payments.Add(payment);

        invoice.AmountPaid += command.Amount;
        invoice.Status = invoice.AmountPaid >= invoice.TotalAmount
            ? InvoiceStatus.Paid
            : InvoiceStatus.PartiallyPaid;

        await db.SaveChangesAsync(ct);

        return payment.Id;
    }
}
