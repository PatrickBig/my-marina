using MyMarina.Domain.Enums;

namespace MyMarina.Domain.ValueObjects;

public sealed class AmenityAddOn
{
    public PlanAmenity Amenity { get; set; }
    public decimal? TransientAmount { get; set; }
    public decimal? LeaseAmount { get; set; }
}
