using MyMarina.Application.Invoicing;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Invoicing;

internal static class InvoiceHelper
{
    public static InvoiceDto ToDto(Invoice i, string marinaName, string billingAccountName) =>
        new(
            Id: i.Id,
            MarinaId: i.MarinaId,
            MarinaName: marinaName,
            BillingAccountId: i.BillingAccountId,
            BillingAccountName: billingAccountName,
            ReservationId: i.ReservationId,
            SlipAssignmentId: i.SlipAssignmentId,
            InvoiceNumber: i.InvoiceNumber,
            Status: i.Status.ToString(),
            IssuedDate: i.IssuedDate,
            DueDate: i.DueDate,
            SubTotal: i.SubTotal,
            TaxAmount: i.TaxAmount,
            TotalAmount: i.TotalAmount,
            AmountPaid: i.AmountPaid,
            BalanceDue: i.TotalAmount - i.AmountPaid,
            Notes: i.Notes,
            CreatedAt: i.CreatedAt,
            LineItems: i.LineItems.Select(ToLineItemDto).ToList(),
            Payments: i.Payments.Select(ToPaymentDto).ToList());

    public static InvoiceSummaryDto ToSummaryDto(Invoice i, string billingAccountName) =>
        new(
            Id: i.Id,
            InvoiceNumber: i.InvoiceNumber,
            Status: i.Status.ToString(),
            BillingAccountName: billingAccountName,
            IssuedDate: i.IssuedDate,
            DueDate: i.DueDate,
            TotalAmount: i.TotalAmount,
            AmountPaid: i.AmountPaid,
            BalanceDue: i.TotalAmount - i.AmountPaid);

    public static InvoiceLineItemDto ToLineItemDto(InvoiceLineItem l) =>
        new(l.Id, l.Description, l.Quantity, l.UnitPrice, l.LineTotal, l.SlipAssignmentId, l.ReservationId);

    public static PaymentDto ToPaymentDto(Payment p) =>
        new(p.Id, p.Amount, p.PaidOn, p.Method.ToString(), p.ReferenceNumber, p.Notes, p.CreatedAt);
}
