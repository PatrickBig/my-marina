using Hangfire;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.PricingPlans;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Pricing;

public class RecomputeSlipPriceJob(AppDbContext db)
{
    [Queue("default")]
    public async Task ExecuteAsync(Guid slipId)
    {
        var slip = await db.Slips
            .IgnoreQueryFilters()
            .Include(s => s.PricingPlan)
            .FirstOrDefaultAsync(s => s.Id == slipId);

        if (slip is null) return;

        PricingPlan? plan = slip.PricingPlan;

        if (plan is null)
        {
            plan = await db.PricingPlans
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.MarinaId == slip.MarinaId && p.IsDefault);
        }

        if (plan is null)
        {
            slip.ResolvedTransientBaseRate = null;
            slip.ResolvedLeaseBaseRate = null;
        }
        else
        {
            var amenities = PriceResolver.SlipAmenities(slip);
            var (transient, lease) = PriceResolver.Resolve(plan, slip.MaxLength, slip.MaxBeam, amenities);
            slip.ResolvedTransientBaseRate = transient;
            slip.ResolvedLeaseBaseRate = lease;
        }

        await db.SaveChangesAsync();
    }
}
