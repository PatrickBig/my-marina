using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoicing;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoicing;

internal sealed class CreateInvoiceCommandHandler(AppDbContext db)
    : ICommandHandler<CreateInvoiceCommand, InvoiceDto>
{
    public async Task<InvoiceDto> HandleAsync(CreateInvoiceCommand command, CancellationToken ct = default)
    {
        var marina = await db.Marinas
            .FirstOrDefaultAsync(m => m.Id == command.MarinaId, ct)
            ?? throw new InvalidOperationException("Marina not found.");

        var billingAccount = await db.BillingAccounts
            .FirstOrDefaultAsync(b => b.Id == command.BillingAccountId && b.MarinaId == command.MarinaId, ct)
            ?? throw new InvalidOperationException("Billing account not found.");

        // Sequential invoice number within a serializable transaction
        var nextNumber = await db.Invoices
            .Where(i => i.MarinaId == command.MarinaId)
            .CountAsync(ct) + 1;

        var invoiceNumber = $"INV-{nextNumber:D4}";

        var lineItems = command.LineItems.Select(l => new InvoiceLineItem
        {
            Description     = l.Description,
            Quantity        = l.Quantity,
            UnitPrice       = l.UnitPrice,
            LineTotal       = Math.Round(l.Quantity * l.UnitPrice, 2),
            SlipAssignmentId = l.SlipAssignmentId,
            ReservationId   = l.ReservationId,
        }).ToList();

        var subTotal    = lineItems.Sum(l => l.LineTotal);
        var totalAmount = subTotal + command.TaxAmount;

        var invoice = new Invoice
        {
            MarinaId         = command.MarinaId,
            BillingAccountId = command.BillingAccountId,
            ReservationId    = command.ReservationId,
            SlipAssignmentId = command.SlipAssignmentId,
            InvoiceNumber    = invoiceNumber,
            IssuedDate       = command.IssuedDate,
            DueDate          = command.DueDate,
            SubTotal         = subTotal,
            TaxAmount        = command.TaxAmount,
            TotalAmount      = totalAmount,
            AmountPaid       = 0m,
            Notes            = command.Notes,
        };

        foreach (var li in lineItems)
            li.InvoiceId = invoice.Id;

        invoice.LineItems = lineItems;

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);

        return InvoiceHelper.ToDto(invoice, marina.Name, billingAccount.DisplayName);
    }
}
