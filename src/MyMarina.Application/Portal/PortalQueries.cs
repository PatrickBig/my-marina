using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.Portal;

// ── Query records ────────────────────────────────────────────────────────────

public sealed record GetPortalMeQuery;
public sealed record GetPortalSlipQuery;
public sealed record GetPortalBoatsQuery;
public sealed record GetPortalInvoicesQuery;
public sealed record GetPortalInvoiceQuery(Guid InvoiceId);
public sealed record GetPortalMaintenanceRequestsQuery;
public sealed record GetPortalAnnouncementsQuery;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record PortalMeDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    Guid CustomerAccountId,
    string AccountDisplayName,
    string BillingEmail,
    string? BillingPhone);

public sealed record PortalSlipAssignmentDto(
    Guid Id,
    Guid SlipId,
    string SlipName,
    string? DockName,
    string MarinaName,
    string BoatName,
    AssignmentType AssignmentType,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal? RateOverride,
    string? Notes);

public sealed record PortalBoatDto(
    Guid Id,
    string Name,
    string? Make,
    string? Model,
    int? Year,
    decimal Length,
    decimal Beam,
    decimal Draft,
    BoatType BoatType,
    string? RegistrationNumber,
    DateOnly? InsuranceExpiresOn);

public sealed record PortalInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    InvoiceStatus Status,
    DateOnly IssuedDate,
    DateOnly DueDate,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceDue,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record PortalInvoiceLineItemDto(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record PortalPaymentDto(
    decimal Amount,
    DateOnly PaidOn,
    PaymentMethod Method,
    string? ReferenceNumber);

public sealed record PortalInvoiceDetailDto(
    Guid Id,
    string InvoiceNumber,
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
    IReadOnlyList<PortalInvoiceLineItemDto> LineItems,
    IReadOnlyList<PortalPaymentDto> Payments);

public sealed record PortalMaintenanceRequestDto(
    Guid Id,
    string Title,
    string Description,
    MaintenanceStatus Status,
    Priority Priority,
    Guid? SlipId,
    string? SlipName,
    Guid? BoatId,
    string? BoatName,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ResolvedAt);

public sealed record PortalAnnouncementDto(
    Guid Id,
    string Title,
    string Body,
    bool IsPinned,
    DateTimeOffset PublishedAt,
    DateTimeOffset? ExpiresAt,
    string MarinaName);

// ── Handler interfaces ────────────────────────────────────────────────────────

public interface IGetPortalMeQueryHandler : IQueryHandler<GetPortalMeQuery, PortalMeDto?>;
public interface IGetPortalSlipQueryHandler : IQueryHandler<GetPortalSlipQuery, PortalSlipAssignmentDto?>;
public interface IGetPortalBoatsQueryHandler : IQueryHandler<GetPortalBoatsQuery, IReadOnlyList<PortalBoatDto>>;
public interface IGetPortalInvoicesQueryHandler : IQueryHandler<GetPortalInvoicesQuery, IReadOnlyList<PortalInvoiceDto>>;
public interface IGetPortalInvoiceQueryHandler : IQueryHandler<GetPortalInvoiceQuery, PortalInvoiceDetailDto?>;
public interface IGetPortalMaintenanceRequestsQueryHandler : IQueryHandler<GetPortalMaintenanceRequestsQuery, IReadOnlyList<PortalMaintenanceRequestDto>>;
public interface IGetPortalAnnouncementsQueryHandler : IQueryHandler<GetPortalAnnouncementsQuery, IReadOnlyList<PortalAnnouncementDto>>;
