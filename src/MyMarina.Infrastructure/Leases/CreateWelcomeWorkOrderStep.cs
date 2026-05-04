using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Leases;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Leases;

public class CreateWelcomeWorkOrderStep(AppDbContext db) : ILeaseOnboardingStep
{
    public string StepKey => "CreateWelcomeWorkOrder";

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

        var slip = await db.Slips.FindAsync([evt.SlipId], ct);
        var title = config.WelcomeWorkOrderTitle.Replace("{slip}", slip?.Name ?? evt.SlipId.ToString());
        var desc  = config.WelcomeWorkOrderDescription.Replace("{slip}", slip?.Name ?? evt.SlipId.ToString());

        db.WorkOrders.Add(new WorkOrder
        {
            MarinaId   = evt.MarinaId,
            Title      = title,
            Description = desc,
            Priority   = MaintenancePriority.Medium,
            Status     = WorkOrderStatus.Open,
            Notes      = $"Auto-created from lease inquiry approval (InquiryId: {evt.InquiryId})",
        });
        await db.SaveChangesAsync(ct);
    }
}
