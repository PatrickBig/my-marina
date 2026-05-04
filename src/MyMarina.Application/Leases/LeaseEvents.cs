namespace MyMarina.Application.Leases;

/// <summary>
/// Published via IMessageBus when a lease inquiry is approved.
/// SlipLeaseApprovedHandler fans out to all registered ILeaseOnboardingStep implementations
/// based on the marina's OnboardingConfig.
/// </summary>
public sealed record SlipLeaseApproved(
    Guid InquiryId,
    Guid SlipId,
    Guid MarinaId,
    Guid SlipAssignmentId,
    Guid BillingAccountId,
    Guid RequestingUserId,
    Guid ApprovedByUserId
);
