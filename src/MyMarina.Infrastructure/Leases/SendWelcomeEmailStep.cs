using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Leases;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Leases;

public class SendWelcomeEmailStep(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IEmailService email)
    : ILeaseOnboardingStep
{
    public string StepKey => "SendWelcomeEmail";

    public async Task ExecuteAsync(SlipLeaseApproved evt, CancellationToken ct)
    {
        var marina = await db.Marinas.FindAsync([evt.MarinaId], ct);
        if (marina == null) return;

        MarinaOnboardingConfig config;
        try
        {
            config = string.IsNullOrWhiteSpace(marina.OnboardingConfig)
                ? new MarinaOnboardingConfig()
                : JsonSerializer.Deserialize<MarinaOnboardingConfig>(marina.OnboardingConfig)
                  ?? new MarinaOnboardingConfig();
        }
        catch { config = new MarinaOnboardingConfig(); }

        var user = await userManager.FindByIdAsync(evt.RequestingUserId.ToString());
        if (user?.Email == null) return;

        var slip      = await db.Slips.FindAsync([evt.SlipId], ct);
        var subject   = config.WelcomeEmailSubject ?? $"Welcome to {marina.Name}!";
        var body      = config.WelcomeEmailBodyTemplate
            ?? $"Hi {user.FirstName},\n\nYour lease for slip {slip?.Name} at {marina.Name} has been approved. Welcome!\n\nThe {marina.Name} Team";

        body = body
            .Replace("{firstName}", user.FirstName ?? "Boater")
            .Replace("{marina}",    marina.Name)
            .Replace("{slip}",      slip?.Name ?? string.Empty);

        await email.SendGenericAsync(user.Email, subject, body, ct);
    }
}
