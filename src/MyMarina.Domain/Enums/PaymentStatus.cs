namespace MyMarina.Domain.Enums;

public enum PaymentStatus
{
    OffPlatform = 0, // MVP default — money moves outside the platform
    Pending     = 1, // Era 2: Stripe hold captured
    Captured    = 2, // Era 2: payment settled
    Refunded    = 3, // Era 2: full or partial refund issued
}
