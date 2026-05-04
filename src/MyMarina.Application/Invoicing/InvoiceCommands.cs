namespace MyMarina.Application.Invoicing;

public sealed record CreateInvoiceLineItemData(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    Guid? SlipAssignmentId,
    Guid? ReservationId);

public sealed record CreateInvoiceCommand(
    Guid MarinaId,
    Guid BillingAccountId,
    Guid? ReservationId,
    Guid? SlipAssignmentId,
    DateOnly IssuedDate,
    DateOnly DueDate,
    decimal TaxAmount,
    string? Notes,
    IReadOnlyList<CreateInvoiceLineItemData> LineItems,
    Guid RequestingUserId);

public sealed record AddLineItemCommand(
    Guid InvoiceId,
    Guid MarinaId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    Guid? SlipAssignmentId,
    Guid? ReservationId,
    Guid RequestingUserId);

public sealed record RemoveLineItemCommand(
    Guid InvoiceId,
    Guid LineItemId,
    Guid MarinaId,
    Guid RequestingUserId);

public sealed record SendInvoiceCommand(
    Guid InvoiceId,
    Guid MarinaId,
    Guid RequestingUserId);

public sealed record VoidInvoiceCommand(
    Guid InvoiceId,
    Guid MarinaId,
    Guid RequestingUserId);

public sealed record RecordPaymentCommand(
    Guid InvoiceId,
    Guid MarinaId,
    decimal Amount,
    DateOnly PaidOn,
    string Method,
    string? ReferenceNumber,
    string? Notes,
    Guid RequestingUserId);

public sealed record GetInvoicesQuery(
    Guid MarinaId,
    string? Status,
    Guid? BillingAccountId);

public sealed record GetInvoiceQuery(
    Guid InvoiceId,
    Guid MarinaId);

public sealed record GetMyInvoicesQuery(
    IReadOnlyList<Guid> BillingAccountIds);
