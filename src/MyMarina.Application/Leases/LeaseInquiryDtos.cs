namespace MyMarina.Application.Leases;

public sealed record LeaseInquiryDto(
    Guid Id,
    Guid SlipId,
    string SlipName,
    Guid MarinaId,
    string MarinaName,
    Guid RequestingUserId,
    string RequestingUserName,
    string RequestingUserEmail,
    Guid? VesselId,
    string? VesselName,
    string DesiredTerm,
    DateOnly? DesiredStartDate,
    string? Message,
    // Marina-editable agreed terms
    string? AgreedRateKind,
    decimal? AgreedBaseRate,
    DateOnly? AssignmentStartDate,
    DateOnly? AssignmentEndDate,
    string? MarinaNote,
    // Status + audit
    string Status,
    string? ReviewedByUserName,
    DateTimeOffset? ReviewedAt,
    string? ApprovedByUserName,
    DateTimeOffset? ApprovedAt,
    string? DeclinedByUserName,
    DateTimeOffset? DeclinedAt,
    // Links set on approval
    Guid? SlipAssignmentId,
    Guid? BillingAccountId,
    DateTimeOffset CreatedAt
);
