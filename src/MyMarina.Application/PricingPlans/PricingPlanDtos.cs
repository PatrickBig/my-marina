using MyMarina.Domain.Enums;

namespace MyMarina.Application.PricingPlans;

public sealed record AmenityAddOnDto(
    PlanAmenity Amenity,
    decimal? TransientAmount,
    decimal? LeaseAmount
);

public sealed record PricingPlanDto(
    Guid Id,
    Guid MarinaId,
    string Name,
    bool IsDefault,
    RateKind? TransientRateKind,
    decimal? TransientAmount,
    decimal? TransientMinCharge,
    RateKind? LeaseRateKind,
    decimal? LeaseAmount,
    decimal? LeaseMinCharge,
    IReadOnlyList<AmenityAddOnDto> AmenityAddOns,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed record BulkAssignResultDto(int AssignedCount);
