using MyMarina.Application.PricingPlans;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Domain.ValueObjects;

namespace MyMarina.UnitTests.Pricing;

public class PriceResolverTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static PricingPlan MakePlan(
        RateKind? transientKind = RateKind.Flat,
        decimal? transientAmount = 100m,
        decimal? transientMin = null,
        RateKind? leaseKind = RateKind.Flat,
        decimal? leaseAmount = 500m,
        decimal? leaseMin = null,
        ICollection<AmenityAddOn>? addOns = null) => new()
    {
        Name = "Test Plan",
        MarinaId = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        TransientRateKind = transientKind,
        TransientAmount = transientAmount,
        TransientMinCharge = transientMin,
        LeaseRateKind = leaseKind,
        LeaseAmount = leaseAmount,
        LeaseMinCharge = leaseMin,
        AmenityAddOns = addOns ?? [],
    };

    private static Slip MakeSlip(
        decimal length = 30m,
        decimal beam = 10m,
        bool covered = false,
        ElectricAmperage? electric = null,
        bool water = false,
        bool pumpOut = false) => new()
    {
        Name = "A1",
        MarinaId = Guid.NewGuid(),
        MaxLength = length,
        MaxBeam = beam,
        IsCovered = covered,
        Electric = electric,
        HasElectric = electric.HasValue,
        HasWater = water,
        HasPumpOut = pumpOut,
    };

    // ── Null rate kind returns null ───────────────────────────────────────

    [Fact]
    public void Resolve_NullTransientRateKind_ReturnsNullForTransient()
    {
        var plan = MakePlan(transientKind: null, transientAmount: null);
        var (transient, lease) = PriceResolver.Resolve(plan, 30m, 10m, []);
        Assert.Null(transient);
        Assert.NotNull(lease);
    }

    [Fact]
    public void Resolve_NullLeaseRateKind_ReturnsNullForLease()
    {
        var plan = MakePlan(leaseKind: null, leaseAmount: null);
        var (transient, lease) = PriceResolver.Resolve(plan, 30m, 10m, []);
        Assert.NotNull(transient);
        Assert.Null(lease);
    }

    // ── Flat rate ─────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_FlatTransient_ReturnsAmount()
    {
        var plan = MakePlan(transientKind: RateKind.Flat, transientAmount: 150m, leaseKind: null, leaseAmount: null);
        var (transient, _) = PriceResolver.Resolve(plan, 30m, 10m, []);
        Assert.Equal(150m, transient);
    }

    [Fact]
    public void Resolve_FlatLease_ReturnsAmount()
    {
        var plan = MakePlan(transientKind: null, transientAmount: null, leaseKind: RateKind.Flat, leaseAmount: 800m);
        var (_, lease) = PriceResolver.Resolve(plan, 30m, 10m, []);
        Assert.Equal(800m, lease);
    }

    // ── PerFoot rate ──────────────────────────────────────────────────────

    [Fact]
    public void Resolve_PerFootTransient_MultipliesByLength()
    {
        var plan = MakePlan(transientKind: RateKind.PerFoot, transientAmount: 5m, leaseKind: null, leaseAmount: null);
        var (transient, _) = PriceResolver.Resolve(plan, 40m, 10m, []);
        Assert.Equal(200m, transient); // 5 * 40
    }

    [Fact]
    public void Resolve_PerFootLease_MultipliesByLength()
    {
        var plan = MakePlan(transientKind: null, transientAmount: null, leaseKind: RateKind.PerFoot, leaseAmount: 20m);
        var (_, lease) = PriceResolver.Resolve(plan, 35m, 12m, []);
        Assert.Equal(700m, lease); // 20 * 35
    }

    // ── PerArea rate ──────────────────────────────────────────────────────

    [Fact]
    public void Resolve_PerAreaTransient_MultipliesByLengthAndBeam()
    {
        var plan = MakePlan(transientKind: RateKind.PerArea, transientAmount: 2m, leaseKind: null, leaseAmount: null);
        var (transient, _) = PriceResolver.Resolve(plan, 30m, 10m, []);
        Assert.Equal(600m, transient); // 2 * 30 * 10
    }

    [Fact]
    public void Resolve_PerAreaLease_MultipliesByLengthAndBeam()
    {
        var plan = MakePlan(transientKind: null, transientAmount: null, leaseKind: RateKind.PerArea, leaseAmount: 3m);
        var (_, lease) = PriceResolver.Resolve(plan, 20m, 8m, []);
        Assert.Equal(480m, lease); // 3 * 20 * 8
    }

    // ── MinCharge floor ───────────────────────────────────────────────────

    [Fact]
    public void Resolve_RawBelowMinCharge_ReturnsMinCharge()
    {
        var plan = MakePlan(transientKind: RateKind.Flat, transientAmount: 50m, transientMin: 100m,
                            leaseKind: null, leaseAmount: null);
        var (transient, _) = PriceResolver.Resolve(plan, 30m, 10m, []);
        Assert.Equal(100m, transient);
    }

    [Fact]
    public void Resolve_RawAboveMinCharge_ReturnsRaw()
    {
        var plan = MakePlan(transientKind: RateKind.Flat, transientAmount: 200m, transientMin: 100m,
                            leaseKind: null, leaseAmount: null);
        var (transient, _) = PriceResolver.Resolve(plan, 30m, 10m, []);
        Assert.Equal(200m, transient);
    }

    // ── Amenity add-ons ───────────────────────────────────────────────────

    [Fact]
    public void Resolve_CoveredAddOn_AddsToTransient()
    {
        var addOns = new List<AmenityAddOn>
        {
            new() { Amenity = PlanAmenity.Covered, TransientAmount = 25m, LeaseAmount = 100m },
        };
        var plan = MakePlan(transientKind: RateKind.Flat, transientAmount: 100m,
                            leaseKind: null, leaseAmount: null, addOns: addOns);

        var (transient, _) = PriceResolver.Resolve(plan, 30m, 10m, [PlanAmenity.Covered]);
        Assert.Equal(125m, transient); // 100 + 25
    }

    [Fact]
    public void Resolve_MultipleAddOns_StackCorrectly()
    {
        var addOns = new List<AmenityAddOn>
        {
            new() { Amenity = PlanAmenity.Covered,  TransientAmount = 20m },
            new() { Amenity = PlanAmenity.HasWater, TransientAmount = 10m },
        };
        var plan = MakePlan(transientKind: RateKind.Flat, transientAmount: 100m,
                            leaseKind: null, leaseAmount: null, addOns: addOns);

        var (transient, _) = PriceResolver.Resolve(plan, 30m, 10m, [PlanAmenity.Covered, PlanAmenity.HasWater]);
        Assert.Equal(130m, transient); // 100 + 20 + 10
    }

    [Fact]
    public void Resolve_AddOnNotApplicable_NotAdded()
    {
        var addOns = new List<AmenityAddOn>
        {
            new() { Amenity = PlanAmenity.Covered, TransientAmount = 25m },
        };
        var plan = MakePlan(transientKind: RateKind.Flat, transientAmount: 100m,
                            leaseKind: null, leaseAmount: null, addOns: addOns);

        // slip is NOT covered — add-on should not apply
        var (transient, _) = PriceResolver.Resolve(plan, 30m, 10m, []);
        Assert.Equal(100m, transient);
    }

    [Fact]
    public void Resolve_AddOnWithNullAmount_TreatedAsZero()
    {
        var addOns = new List<AmenityAddOn>
        {
            new() { Amenity = PlanAmenity.HasPumpOut, TransientAmount = null },
        };
        var plan = MakePlan(transientKind: RateKind.Flat, transientAmount: 100m,
                            leaseKind: null, leaseAmount: null, addOns: addOns);

        var (transient, _) = PriceResolver.Resolve(plan, 30m, 10m, [PlanAmenity.HasPumpOut]);
        Assert.Equal(100m, transient);
    }

    // ── SlipAmenities helper ──────────────────────────────────────────────

    [Fact]
    public void SlipAmenities_CoveredSlip_YieldsCovered()
    {
        var slip = MakeSlip(covered: true);
        Assert.Contains(PlanAmenity.Covered, PriceResolver.SlipAmenities(slip));
    }

    [Fact]
    public void SlipAmenities_30AElectric_YieldsElectric30A()
    {
        var slip = MakeSlip(electric: ElectricAmperage.Amp30);
        Assert.Contains(PlanAmenity.Electric30A, PriceResolver.SlipAmenities(slip));
        Assert.DoesNotContain(PlanAmenity.Electric50A, PriceResolver.SlipAmenities(slip));
    }

    [Fact]
    public void SlipAmenities_50AElectric_YieldsElectric50A()
    {
        var slip = MakeSlip(electric: ElectricAmperage.Amp50);
        Assert.Contains(PlanAmenity.Electric50A, PriceResolver.SlipAmenities(slip));
        Assert.DoesNotContain(PlanAmenity.Electric30A, PriceResolver.SlipAmenities(slip));
    }

    [Fact]
    public void SlipAmenities_Water_YieldsHasWater()
    {
        var slip = MakeSlip(water: true);
        Assert.Contains(PlanAmenity.HasWater, PriceResolver.SlipAmenities(slip));
    }

    [Fact]
    public void SlipAmenities_PumpOut_YieldsHasPumpOut()
    {
        var slip = MakeSlip(pumpOut: true);
        Assert.Contains(PlanAmenity.HasPumpOut, PriceResolver.SlipAmenities(slip));
    }

    [Fact]
    public void SlipAmenities_NoAmenities_YieldsEmpty()
    {
        var slip = MakeSlip();
        Assert.Empty(PriceResolver.SlipAmenities(slip));
    }
}
