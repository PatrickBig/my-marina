namespace MyMarina.Domain.Enums;

public enum RateKind
{
    Flat   = 0,   // fixed amount per period
    PerFoot = 1,  // amount × vessel LOA per period
}
