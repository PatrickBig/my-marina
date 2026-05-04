using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoicing;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoicing;

internal sealed class RecordPaymentCommandHandler(AppDbContext db)
    : ICommandHandler<RecordPaymentCommand, PaymentDto>
{
    public async Task<PaymentDto> HandleAsync(RecordPaymentCommand command, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .FirstOrDefaultAsync(i => i.Id == command.InvoiceId && i.MarinaId == command.MarinaId, ct)
            ?? throw new InvalidOperationException("Invoice not found.");

        if (invoice.Status is InvoiceStatus.Voided)
            throw new InvalidOperationException("Cannot record payment on a voided invoice.");

        if (!Enum.TryParse<PaymentMethod>(command.Method, ignoreCase: true, out var method))
            throw new InvalidOperationException($"Unknown payment method: {command.Method}");

        var payment = new Payment
        {
            InvoiceId           = command.InvoiceId,
            Amount              = command.Amount,
            PaidOn              = command.PaidOn,
            Method              = method,
            ReferenceNumber     = command.ReferenceNumber,
            Notes               = command.Notes,
            RecordedByUserId    = command.RequestingUserId,
        };

        db.Payments.Add(payment);

        invoice.AmountPaid += command.Amount;
        invoice.Status = invoice.AmountPaid >= invoice.TotalAmount
            ? InvoiceStatus.Paid
            : InvoiceStatus.PartiallyPaid;

        await db.SaveChangesAsync(ct);

        return InvoiceHelper.ToPaymentDto(payment);
    }
}
