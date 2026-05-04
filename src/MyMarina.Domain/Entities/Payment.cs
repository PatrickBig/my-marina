using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

public class Payment
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PaidOn { get; set; }
    public PaymentMethod Method { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public string? PaymentProviderId { get; set; }
    public string? PaymentProviderReference { get; set; }
    public Guid RecordedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    // Navigation
    public Invoice Invoice { get; set; } = null!;
}
