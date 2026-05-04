namespace MyMarina.Domain.Enums;

public enum ListedByKind
{
    Owner        = 0, // Slip's marina lists directly
    Holder       = 1, // Long-term tenant sublets their leased slip
    OwnerForHolder = 2, // Marina sublets on the holder's behalf while they're away
}
