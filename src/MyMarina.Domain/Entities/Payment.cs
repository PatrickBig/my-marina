using MyMarina.Domain.Common;
using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

/// <summary>
/// A payment applied to an invoice. Manual recording in MVP.
/// PaymentProviderId and PaymentProviderReference are reserved for
/// future payment processor integration.
/// </summary>
public class Payment : TenantEntity
{
    public Guid InvoiceId { get; init; }
    public decimal Amount { get; set; }
    public DateOnly PaidOn { get; set; }
    public PaymentMethod Method { get; set; }

    /// <summary>Check number, transaction ID, etc.</summary>
    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }

    /// <summary>Reserved for future payment processor integration.</summary>
    public string? PaymentProviderId { get; set; }

    /// <summary>External transaction ID from the payment provider.</summary>
    public string? PaymentProviderReference { get; set; }

    public Guid RecordedByUserId { get; init; }

    public Invoice Invoice { get; init; } = null!;
}
