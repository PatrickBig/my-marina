namespace MyMarina.Domain.Enums;

public enum ReservationStatus
{
    PendingApproval            = 0,
    PendingHostMarinaApproval  = 1,
    Confirmed                  = 2,
    Declined                   = 3,
    Cancelled                  = 4,
    Completed                  = 5,
    NoShow                     = 6,
}
