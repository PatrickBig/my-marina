using Hangfire;
using Microsoft.EntityFrameworkCore;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Pricing;

public class RecomputeDefaultPlanSlipPricesJob(AppDbContext db)
{
    [Queue("default")]
    public async Task ExecuteAsync(Guid marinaId)
    {
        var slipIds = await db.Slips
            .IgnoreQueryFilters()
            .Where(s => s.MarinaId == marinaId && s.PricingPlanId == null)
            .Select(s => s.Id)
            .ToListAsync();

        foreach (var slipId in slipIds)
            BackgroundJob.Enqueue<RecomputeSlipPriceJob>(j => j.ExecuteAsync(slipId));
    }
}
