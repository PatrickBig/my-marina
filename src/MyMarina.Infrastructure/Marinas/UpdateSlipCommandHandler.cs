using Hangfire;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Infrastructure.Persistence;
using MyMarina.Infrastructure.Pricing;

namespace MyMarina.Infrastructure.Marinas;

public class UpdateSlipCommandHandler(AppDbContext db)
    : ICommandHandler<UpdateSlipCommand, SlipDto>
{
    public async Task<SlipDto> HandleAsync(UpdateSlipCommand command, CancellationToken ct = default)
    {
        var slip = await db.Slips
            .FirstOrDefaultAsync(s => s.Id == command.SlipId && s.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException("Slip not found.");

        if (command.DockId is not null) slip.DockId = command.DockId;
        if (command.Name is not null) slip.Name = command.Name;
        if (command.SlipType.HasValue) slip.SlipType = command.SlipType.Value;
        if (command.MaxLength.HasValue) slip.MaxLength = command.MaxLength.Value;
        if (command.MaxBeam.HasValue) slip.MaxBeam = command.MaxBeam.Value;
        if (command.MaxDraft.HasValue) slip.MaxDraft = command.MaxDraft.Value;
        if (command.HasElectric.HasValue) slip.HasElectric = command.HasElectric.Value;
        if (command.Electric is not null) slip.Electric = command.Electric;
        if (command.HasWater.HasValue) slip.HasWater = command.HasWater.Value;
        if (command.HasPumpOut.HasValue) slip.HasPumpOut = command.HasPumpOut.Value;
        if (command.IsCovered.HasValue) slip.IsCovered = command.IsCovered.Value;
        if (command.IsIndoor.HasValue) slip.IsIndoor = command.IsIndoor.Value;
        if (command.Amenities is not null) slip.Amenities = [.. command.Amenities];
        if (command.Status.HasValue) slip.Status = command.Status.Value;
        if (command.Notes is not null) slip.Notes = command.Notes;

        bool planChanged = false;
        if (command.ClearPricingPlan && slip.PricingPlanId is not null)
        {
            slip.PricingPlanId = null;
            planChanged = true;
        }
        else if (command.PricingPlanId.HasValue && command.PricingPlanId != slip.PricingPlanId)
        {
            var planBelongsToMarina = await db.PricingPlans
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Id == command.PricingPlanId && p.MarinaId == command.MarinaId, ct);
            if (!planBelongsToMarina)
                throw new InvalidOperationException("Pricing plan does not belong to this marina.");
            slip.PricingPlanId = command.PricingPlanId;
            planChanged = true;
        }

        await db.SaveChangesAsync(ct);

        if (planChanged)
            BackgroundJob.Enqueue<RecomputeSlipPriceJob>(j => j.ExecuteAsync(slip.Id));

        return MarinaMappers.ToSlipDto(slip);
    }
}
