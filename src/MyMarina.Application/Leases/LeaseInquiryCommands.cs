using MyMarina.Domain.Enums;

namespace MyMarina.Application.Leases;

// Boater submits a lease inquiry for a slip
public sealed record CreateLeaseInquiryCommand(
    Guid SlipId,
    Guid RequestingUserId,
    Guid? VesselId,
    LeaseTerm DesiredTerm,
    DateOnly? DesiredStartDate,
    string? Message
);

// Marina updates terms or notes before approving
public sealed record UpdateLeaseInquiryCommand(
    Guid InquiryId,
    Guid MarinaId,
    Guid RequestingUserId,
    RateKind? AgreedRateKind,
    decimal? AgreedBaseRate,
    DateOnly? AssignmentStartDate,
    DateOnly? AssignmentEndDate,
    string? MarinaNote,
    bool MarkUnderReview    // set to true to move status to UnderReview
);

// Marina approves — system creates SlipAssignment + BillingAccount automatically
public sealed record ApproveLeaseInquiryCommand(
    Guid InquiryId,
    Guid MarinaId,
    Guid ApprovingUserId
);

// Marina declines
public sealed record DeclineLeaseInquiryCommand(
    Guid InquiryId,
    Guid MarinaId,
    Guid DecliningUserId,
    string? Reason
);

// Boater withdraws their own inquiry
public sealed record WithdrawLeaseInquiryCommand(
    Guid InquiryId,
    Guid RequestingUserId
);
