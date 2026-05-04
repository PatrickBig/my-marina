namespace MyMarina.Application.Platform;

public record CreateTenantCommand(
    string Name,
    string Slug,
    string? BillingEmail,
    string SubscriptionTier = "Free");

public record SuspendTenantCommand(Guid TenantId, string? Reason);
public record ReactivateTenantCommand(Guid TenantId);

public record ForceSignOutCommand(Guid TargetUserId);
public record DeactivateUserCommand(Guid TargetUserId, string? Reason);
public record ActivateUserCommand(Guid TargetUserId);

public record RemoveListingCommand(Guid ListingId, string? Reason);
