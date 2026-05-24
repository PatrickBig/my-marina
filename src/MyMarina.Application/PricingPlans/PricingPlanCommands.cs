using MyMarina.Domain.Enums;

namespace MyMarina.Application.PricingPlans;

public sealed record CreatePricingPlanCommand(
    Guid MarinaId,
    Guid RequestingUserId,
    string Name,
    bool IsDefault,
    RateKind? TransientRateKind,
    decimal? TransientAmount,
    decimal? TransientMinCharge,
    RateKind? LeaseRateKind,
    decimal? LeaseAmount,
    decimal? LeaseMinCharge,
    IReadOnlyList<AmenityAddOnDto> AmenityAddOns
);

public sealed record UpdatePricingPlanCommand(
    Guid PlanId,
    Guid MarinaId,
    Guid RequestingUserId,
    string? Name,
    RateKind? TransientRateKind,
    decimal? TransientAmount,
    decimal? TransientMinCharge,
    bool ClearTransientRate,
    RateKind? LeaseRateKind,
    decimal? LeaseAmount,
    decimal? LeaseMinCharge,
    bool ClearLeaseRate,
    IReadOnlyList<AmenityAddOnDto>? AmenityAddOns
);

public sealed record DeletePricingPlanCommand(Guid PlanId, Guid MarinaId, Guid RequestingUserId);

public sealed record SetDefaultPricingPlanCommand(Guid PlanId, Guid MarinaId, Guid RequestingUserId);

public sealed record BulkAssignPricingPlanCommand(
    Guid MarinaId,
    Guid RequestingUserId,
    Guid TargetPlanId,
    Guid? DockId,
    decimal? MinLength,
    decimal? MaxLength,
    decimal? MinBeam,
    decimal? MaxBeam,
    IReadOnlyList<PlanAmenity>? Amenities,
    // null = no filter on currentPlanId; Guid.Empty = only slips with PricingPlanId IS NULL
    Guid? CurrentPlanId,
    bool FilterUnassigned
);
