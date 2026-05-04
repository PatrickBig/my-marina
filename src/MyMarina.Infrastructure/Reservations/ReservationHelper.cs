using MyMarina.Application.Reservations;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Reservations;

internal static class ReservationHelper
{
    internal static (decimal BasePrice, decimal Fees, decimal Taxes, decimal Total) ComputePrice(
        AvailabilityWindow window, DateTimeOffset arrivesAt, DateTimeOffset departsAt)
    {
        var nights = (int)(departsAt.Date - arrivesAt.Date).TotalDays;
        if (nights <= 0) nights = 1;

        var basePrice = window.BasePricePerNight * nights;

        var discount = nights >= 28 && window.MonthlyDiscount.HasValue ? window.MonthlyDiscount.Value
                     : nights >= 7  && window.WeeklyDiscount.HasValue  ? window.WeeklyDiscount.Value
                     : 0m;

        var discountedBase = basePrice * (1 - discount);
        var fees           = window.CleaningFee ?? 0m;
        const decimal taxes = 0m; // MVP: no tax configuration
        var total          = discountedBase + fees + taxes;

        return (Math.Round(basePrice, 2), Math.Round(fees, 2), taxes, Math.Round(total, 2));
    }

    internal static ReservationDto ToDto(Reservation r) => new(
        Id:                  r.Id,
        BoaterUserId:        r.BoaterUserId,
        VesselId:            r.VesselId,
        VesselName:          r.Vessel?.Name ?? string.Empty,
        SlipId:              r.SlipId,
        SlipName:            r.Slip?.Name ?? string.Empty,
        MarinaId:            r.Slip?.MarinaId ?? Guid.Empty,
        MarinaName:          string.Empty, // populated by queries that join Marina
        AvailabilityWindowId: r.AvailabilityWindowId,
        ArrivesAt:           r.ArrivesAt,
        DepartsAt:           r.DepartsAt,
        Nights:              (int)(r.DepartsAt.Date - r.ArrivesAt.Date).TotalDays,
        Status:              r.Status.ToString(),
        BasePrice:           r.BasePrice,
        Fees:                r.Fees,
        Taxes:               r.Taxes,
        Total:               r.Total,
        PaymentStatus:       r.PaymentStatus.ToString(),
        InstantBook:         r.AvailabilityWindow?.InstantBook ?? false,
        RequestedAt:         r.RequestedAt,
        ConfirmedAt:         r.ConfirmedAt,
        DeclinedAt:          r.DeclinedAt,
        CancelledAt:         r.CancelledAt,
        CancelledByUserId:   r.CancelledByUserId,
        Notes:               r.Notes
    );

    internal static ReservationDto ToDtoWithMarina(Reservation r, string marinaName) => ToDto(r) with
    {
        MarinaName = marinaName
    };
}
