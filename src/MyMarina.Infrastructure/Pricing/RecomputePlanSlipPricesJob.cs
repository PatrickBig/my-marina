using Hangfire;
using Microsoft.EntityFrameworkCore;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Pricing;

public class RecomputePlanSlipPricesJob(AppDbContext db)
{
    [Queue("default")]
    public async Task ExecuteAsync(Guid planId)
    {
        var slipIds = await db.Slips
            .IgnoreQueryFilters()
            .Where(s => s.PricingPlanId == planId)
            .Select(s => s.Id)
            .ToListAsync();

        foreach (var slipId in slipIds)
            BackgroundJob.Enqueue<RecomputeSlipPriceJob>(j => j.ExecuteAsync(slipId));
    }
}
