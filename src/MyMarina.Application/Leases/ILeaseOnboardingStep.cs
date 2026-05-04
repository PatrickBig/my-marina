namespace MyMarina.Application.Leases;

/// <summary>
/// Represents a single configurable action that runs after a lease inquiry is approved.
/// Implementations are registered in DI and conditionally executed by SlipLeaseApprovedHandler
/// based on the marina's OnboardingConfig JSON blob.
/// </summary>
public interface ILeaseOnboardingStep
{
    string StepKey { get; }
    Task ExecuteAsync(SlipLeaseApproved evt, CancellationToken ct);
}
