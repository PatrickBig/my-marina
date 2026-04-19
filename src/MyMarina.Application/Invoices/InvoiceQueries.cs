using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.Invoices;

/// <param name="CustomerAccountId">Filter to a specific customer. Null = all customers.</param>
/// <param name="Status">Filter by status. Null = all statuses.</param>
/// <param name="IssuedFrom">Inclusive lower bound on IssuedDate.</param>
/// <param name="IssuedTo">Inclusive upper bound on IssuedDate.</param>
/// <param name="MarinaId">Filter to a specific marina. Null = all marinas in tenant.</param>
/// <param name="SortBy">"issuedDate" (default) or "dueDate".</param>
/// <param name="SortDescending">True = newest/latest first. Default true for issuedDate, false for dueDate.</param>
public sealed record GetInvoicesQuery(
    Guid? CustomerAccountId,
    InvoiceStatus? Status,
    DateOnly? IssuedFrom,
    DateOnly? IssuedTo,
    Guid? MarinaId = null,
    string? SortBy = null,
    bool SortDescending = true);

public sealed record GetInvoiceQuery(Guid InvoiceId);

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerAccountId,
    string CustomerDisplayName,
    InvoiceStatus Status,
    DateOnly IssuedDate,
    DateOnly DueDate,
    decimal SubTotal,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceDue,
    string? Notes,
    DateTimeOffset CreatedAt,
    Guid MarinaId);

public sealed record InvoiceLineItemDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    Guid? SlipAssignmentId);

public sealed record PaymentDto(
    Guid Id,
    decimal Amount,
    DateOnly PaidOn,
    PaymentMethod Method,
    string? ReferenceNumber,
    string? Notes,
    Guid RecordedByUserId,
    DateTimeOffset CreatedAt);

public sealed record InvoiceDetailDto(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerAccountId,
    string CustomerDisplayName,
    InvoiceStatus Status,
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

// ── Handler interfaces ────────────────────────────────────────────────────────

public interface IGetInvoicesQueryHandler : IQueryHandler<GetInvoicesQuery, IReadOnlyList<InvoiceDto>>;
public interface IGetInvoiceQueryHandler : IQueryHandler<GetInvoiceQuery, InvoiceDetailDto?>;
