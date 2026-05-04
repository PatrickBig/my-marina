namespace MyMarina.Application.Invoicing;

public sealed record InvoiceLineItemDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    Guid? SlipAssignmentId,
    Guid? ReservationId);

public sealed record PaymentDto(
    Guid Id,
    decimal Amount,
    DateOnly PaidOn,
    string Method,
    string? ReferenceNumber,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record InvoiceDto(
    Guid Id,
    Guid MarinaId,
    string MarinaName,
    Guid BillingAccountId,
    string BillingAccountName,
    Guid? ReservationId,
    Guid? SlipAssignmentId,
    string InvoiceNumber,
    string Status,
    DateOnly IssuedDate,
    DateOnly DueDate,
    decimal SubTotal,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceDue,
    string? Notes,
    DateTimeOffset CreatedAt,
    IReadOnlyList<InvoiceLineItemDto> LineItems,
    IReadOnlyList<PaymentDto> Payments);

public sealed record InvoiceSummaryDto(
    Guid Id,
    string InvoiceNumber,
    string Status,
    string BillingAccountName,
    DateOnly IssuedDate,
    DateOnly DueDate,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceDue);
