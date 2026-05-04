using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Leases;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Leases;

/// <summary>
/// Fans out post-approval onboarding steps based on the marina's OnboardingConfig JSON.
/// Each registered ILeaseOnboardingStep runs if its StepKey is enabled in the config.
/// </summary>
public class SlipLeaseApprovedHandler(
    AppDbContext db,
    IEnumerable<ILeaseOnboardingStep> steps)
    : IMessageHandler<SlipLeaseApproved>
{
    public async Task HandleAsync(SlipLeaseApproved message, CancellationToken ct = default)
    {
        var marina = await db.Marinas.FindAsync([message.MarinaId], ct);
        if (marina == null) return;

        MarinaOnboardingConfig config;
        try
        {
            config = string.IsNullOrWhiteSpace(marina.OnboardingConfig)
                ? new MarinaOnboardingConfig()
                : JsonSerializer.Deserialize<MarinaOnboardingConfig>(marina.OnboardingConfig)
                  ?? new MarinaOnboardingConfig();
        }
        catch
        {
            config = new MarinaOnboardingConfig();
        }

        var enabledKeys = new HashSet<string>();
        if (config.CreateWelcomeWorkOrder) enabledKeys.Add("CreateWelcomeWorkOrder");
        if (config.SendWelcomeEmail)       enabledKeys.Add("SendWelcomeEmail");

        foreach (var step in steps.Where(s => enabledKeys.Contains(s.StepKey)))
        {
            try { await step.ExecuteAsync(message, ct); }
            catch { /* individual step failure must not abort others */ }
        }
    }
}
