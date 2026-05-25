using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.PricingPlans;

public static class PriceResolver
{
    public static (decimal? Transient, decimal? Lease) Resolve(
        PricingPlan plan,
        decimal slipLength,
        decimal slipBeam,
        IEnumerable<PlanAmenity> slipAmenities)
    {
        var amenitySet = slipAmenities as ICollection<PlanAmenity> ?? slipAmenities.ToHashSet();

        return (
            Resolve(plan.TransientRateKind, plan.TransientAmount, plan.TransientMinCharge,
                    slipLength, slipBeam, amenitySet, transient: true, plan),
            Resolve(plan.LeaseRateKind, plan.LeaseAmount, plan.LeaseMinCharge,
                    slipLength, slipBeam, amenitySet, transient: false, plan)
        );
    }

    static decimal? Resolve(
        RateKind? rateKind,
        decimal? amount,
        decimal? minCharge,
        decimal slipLength,
        decimal slipBeam,
        ICollection<PlanAmenity> amenitySet,
        bool transient,
        PricingPlan plan)
    {
        if (rateKind is null || amount is null)
            return null;

        var baseRate = rateKind switch
        {
            RateKind.Flat    => amount.Value,
            RateKind.PerFoot => amount.Value * slipLength,
            RateKind.PerArea => amount.Value * slipLength * slipBeam,
            _                => throw new ArgumentOutOfRangeException(nameof(rateKind)),
        };

        var addOns = plan.AmenityAddOns
            .Where(a => amenitySet.Contains(a.Amenity))
            .Sum(a => transient ? (a.TransientAmount ?? 0m) : (a.LeaseAmount ?? 0m));

        var total = baseRate + addOns;
        return minCharge.HasValue ? Math.Max(total, minCharge.Value) : total;
    }

    public static IEnumerable<PlanAmenity> SlipAmenities(Domain.Entities.Slip slip)
    {
        if (slip.IsCovered) yield return PlanAmenity.Covered;
        if (slip.Electric == ElectricAmperage.Amp30) yield return PlanAmenity.Electric30A;
        if (slip.Electric == ElectricAmperage.Amp50) yield return PlanAmenity.Electric50A;
        if (slip.HasWater) yield return PlanAmenity.HasWater;
        if (slip.HasPumpOut) yield return PlanAmenity.HasPumpOut;
    }
}
