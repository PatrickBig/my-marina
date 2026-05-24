using Hangfire;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.PricingPlans;
using MyMarina.Domain.Entities;
using MyMarina.Domain.ValueObjects;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Pricing;

// ─── Mappers ─────────────────────────────────────────────────────────────────

static class PricingPlanMapper
{
    internal static PricingPlanDto ToDto(PricingPlan p) => new(
        p.Id, p.MarinaId, p.Name, p.IsDefault,
        p.TransientRateKind, p.TransientAmount, p.TransientMinCharge,
        p.LeaseRateKind, p.LeaseAmount, p.LeaseMinCharge,
        p.AmenityAddOns.Select(a => new AmenityAddOnDto(a.Amenity, a.TransientAmount, a.LeaseAmount)).ToList(),
        p.CreatedAt, p.UpdatedAt
    );
}

// ─── Queries ─────────────────────────────────────────────────────────────────

public class ListPricingPlansQueryHandler(AppDbContext db)
    : IQueryHandler<ListPricingPlansQuery, IReadOnlyList<PricingPlanDto>>
{
    public async Task<IReadOnlyList<PricingPlanDto>> HandleAsync(ListPricingPlansQuery query, CancellationToken ct = default)
    {
        var plans = await db.PricingPlans
            .IgnoreQueryFilters()
            .Where(p => p.MarinaId == query.MarinaId)
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);

        return plans.Select(PricingPlanMapper.ToDto).ToList();
    }
}

public class GetPricingPlanQueryHandler(AppDbContext db)
    : IQueryHandler<GetPricingPlanQuery, PricingPlanDto>
{
    public async Task<PricingPlanDto> HandleAsync(GetPricingPlanQuery query, CancellationToken ct = default)
    {
        var plan = await db.PricingPlans
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == query.PlanId && p.MarinaId == query.MarinaId, ct)
            ?? throw new KeyNotFoundException("Pricing plan not found.");

        return PricingPlanMapper.ToDto(plan);
    }
}

// ─── Commands ────────────────────────────────────────────────────────────────

public class CreatePricingPlanCommandHandler(AppDbContext db)
    : ICommandHandler<CreatePricingPlanCommand, PricingPlanDto>
{
    public async Task<PricingPlanDto> HandleAsync(CreatePricingPlanCommand command, CancellationToken ct = default)
    {
        var marina = await db.Marinas
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == command.MarinaId, ct)
            ?? throw new KeyNotFoundException("Marina not found.");

        if (command.IsDefault)
        {
            var existing = await db.PricingPlans
                .IgnoreQueryFilters()
                .Where(p => p.MarinaId == command.MarinaId && p.IsDefault)
                .ToListAsync(ct);
            foreach (var e in existing) e.IsDefault = false;
        }

        var plan = new PricingPlan
        {
            MarinaId = command.MarinaId,
            TenantId = marina.TenantId,
            Name = command.Name,
            IsDefault = command.IsDefault,
            TransientRateKind = command.TransientRateKind,
            TransientAmount = command.TransientAmount,
            TransientMinCharge = command.TransientMinCharge,
            LeaseRateKind = command.LeaseRateKind,
            LeaseAmount = command.LeaseAmount,
            LeaseMinCharge = command.LeaseMinCharge,
            AmenityAddOns = command.AmenityAddOns.Select(a => new AmenityAddOn
            {
                Amenity = a.Amenity,
                TransientAmount = a.TransientAmount,
                LeaseAmount = a.LeaseAmount,
            }).ToList(),
        };

        db.PricingPlans.Add(plan);
        await db.SaveChangesAsync(ct);

        if (command.IsDefault)
            BackgroundJob.Enqueue<RecomputeDefaultPlanSlipPricesJob>(j => j.ExecuteAsync(command.MarinaId));

        return PricingPlanMapper.ToDto(plan);
    }
}

public class UpdatePricingPlanCommandHandler(AppDbContext db)
    : ICommandHandler<UpdatePricingPlanCommand, PricingPlanDto>
{
    public async Task<PricingPlanDto> HandleAsync(UpdatePricingPlanCommand command, CancellationToken ct = default)
    {
        var plan = await db.PricingPlans
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == command.PlanId && p.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException("Pricing plan not found.");

        if (command.Name is not null) plan.Name = command.Name;

        if (command.ClearTransientRate)
        {
            plan.TransientRateKind = null;
            plan.TransientAmount = null;
            plan.TransientMinCharge = null;
        }
        else
        {
            if (command.TransientRateKind.HasValue) plan.TransientRateKind = command.TransientRateKind;
            if (command.TransientAmount.HasValue) plan.TransientAmount = command.TransientAmount;
            if (command.TransientMinCharge.HasValue) plan.TransientMinCharge = command.TransientMinCharge;
        }

        if (command.ClearLeaseRate)
        {
            plan.LeaseRateKind = null;
            plan.LeaseAmount = null;
            plan.LeaseMinCharge = null;
        }
        else
        {
            if (command.LeaseRateKind.HasValue) plan.LeaseRateKind = command.LeaseRateKind;
            if (command.LeaseAmount.HasValue) plan.LeaseAmount = command.LeaseAmount;
            if (command.LeaseMinCharge.HasValue) plan.LeaseMinCharge = command.LeaseMinCharge;
        }

        if (command.AmenityAddOns is not null)
        {
            plan.AmenityAddOns = command.AmenityAddOns.Select(a => new AmenityAddOn
            {
                Amenity = a.Amenity,
                TransientAmount = a.TransientAmount,
                LeaseAmount = a.LeaseAmount,
            }).ToList();
        }

        plan.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        BackgroundJob.Enqueue<RecomputePlanSlipPricesJob>(j => j.ExecuteAsync(plan.Id));
        if (plan.IsDefault)
            BackgroundJob.Enqueue<RecomputeDefaultPlanSlipPricesJob>(j => j.ExecuteAsync(plan.MarinaId));

        return PricingPlanMapper.ToDto(plan);
    }
}

public class DeletePricingPlanCommandHandler(AppDbContext db)
    : ICommandHandler<DeletePricingPlanCommand>
{
    public async Task HandleAsync(DeletePricingPlanCommand command, CancellationToken ct = default)
    {
        var plan = await db.PricingPlans
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == command.PlanId && p.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException("Pricing plan not found.");

        if (plan.IsDefault)
            throw new InvalidOperationException("Cannot delete the active default pricing plan.");

        // Reassign slips to null (they fall back to the default)
        var assignedSlips = await db.Slips
            .IgnoreQueryFilters()
            .Where(s => s.PricingPlanId == plan.Id)
            .ToListAsync(ct);

        foreach (var slip in assignedSlips)
            slip.PricingPlanId = null;

        db.PricingPlans.Remove(plan);
        await db.SaveChangesAsync(ct);

        // Recompute affected slips against the default
        foreach (var slip in assignedSlips)
            BackgroundJob.Enqueue<RecomputeSlipPriceJob>(j => j.ExecuteAsync(slip.Id));
    }
}

public class SetDefaultPricingPlanCommandHandler(AppDbContext db)
    : ICommandHandler<SetDefaultPricingPlanCommand, PricingPlanDto>
{
    public async Task<PricingPlanDto> HandleAsync(SetDefaultPricingPlanCommand command, CancellationToken ct = default)
    {
        var plan = await db.PricingPlans
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == command.PlanId && p.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException("Pricing plan not found.");

        if (!plan.IsDefault)
        {
            var current = await db.PricingPlans
                .IgnoreQueryFilters()
                .Where(p => p.MarinaId == command.MarinaId && p.IsDefault)
                .ToListAsync(ct);

            foreach (var c in current) c.IsDefault = false;
            plan.IsDefault = true;
            plan.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        // Recompute all unassigned slips against the new default
        BackgroundJob.Enqueue<RecomputeDefaultPlanSlipPricesJob>(j => j.ExecuteAsync(command.MarinaId));

        return PricingPlanMapper.ToDto(plan);
    }
}

public class BulkAssignPricingPlanCommandHandler(AppDbContext db)
    : ICommandHandler<BulkAssignPricingPlanCommand, BulkAssignResultDto>
{
    public async Task<BulkAssignResultDto> HandleAsync(BulkAssignPricingPlanCommand command, CancellationToken ct = default)
    {
        // Verify target plan exists in this marina
        var planExists = await db.PricingPlans
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Id == command.TargetPlanId && p.MarinaId == command.MarinaId, ct);
        if (!planExists) throw new KeyNotFoundException("Target pricing plan not found.");

        var query = db.Slips
            .IgnoreQueryFilters()
            .Where(s => s.MarinaId == command.MarinaId);

        if (command.DockId.HasValue)
            query = query.Where(s => s.DockId == command.DockId);
        if (command.MinLength.HasValue)
            query = query.Where(s => s.MaxLength >= command.MinLength);
        if (command.MaxLength.HasValue)
            query = query.Where(s => s.MaxLength <= command.MaxLength);
        if (command.MinBeam.HasValue)
            query = query.Where(s => s.MaxBeam >= command.MinBeam);
        if (command.MaxBeam.HasValue)
            query = query.Where(s => s.MaxBeam <= command.MaxBeam);

        if (command.FilterUnassigned)
            query = query.Where(s => s.PricingPlanId == null);
        else if (command.CurrentPlanId.HasValue)
            query = query.Where(s => s.PricingPlanId == command.CurrentPlanId);

        if (command.Amenities is { Count: > 0 })
        {
            foreach (var amenity in command.Amenities)
            {
                query = amenity switch
                {
                    Domain.Enums.PlanAmenity.Covered     => query.Where(s => s.IsCovered),
                    Domain.Enums.PlanAmenity.Electric30A => query.Where(s => s.Electric == Domain.Enums.ElectricAmperage.Amp30),
                    Domain.Enums.PlanAmenity.Electric50A => query.Where(s => s.Electric == Domain.Enums.ElectricAmperage.Amp50),
                    Domain.Enums.PlanAmenity.HasWater    => query.Where(s => s.HasWater),
                    Domain.Enums.PlanAmenity.HasPumpOut  => query.Where(s => s.HasPumpOut),
                    _                                    => query,
                };
            }
        }

        var slips = await query.ToListAsync(ct);

        foreach (var slip in slips)
            slip.PricingPlanId = command.TargetPlanId;

        await db.SaveChangesAsync(ct);

        foreach (var slip in slips)
            BackgroundJob.Enqueue<RecomputeSlipPriceJob>(j => j.ExecuteAsync(slip.Id));

        return new BulkAssignResultDto(slips.Count);
    }
}
