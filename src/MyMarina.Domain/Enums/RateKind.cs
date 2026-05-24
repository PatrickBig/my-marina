namespace MyMarina.Domain.Enums;

public enum RateKind
{
    Flat    = 0,  // fixed amount per period
    PerFoot = 1,  // amount × slip MaxLength per period
    PerArea = 2,  // amount × slip MaxLength × MaxBeam per period
}
