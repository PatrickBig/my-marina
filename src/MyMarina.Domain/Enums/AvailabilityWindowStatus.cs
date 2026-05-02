namespace MyMarina.Domain.Enums;

public enum AvailabilityWindowStatus
{
    Open        = 0, // Accepting bookings
    Paused      = 1, // Host temporarily hidden from search
    FullyBooked = 2, // All dates consumed by confirmed reservations
    Closed      = 3, // Host closed the window; no new bookings
}
