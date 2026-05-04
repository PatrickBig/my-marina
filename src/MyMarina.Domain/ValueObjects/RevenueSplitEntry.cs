namespace MyMarina.Domain.ValueObjects;

public class RevenueSplitEntry
{
    // PayeeKind ∈ { SlipOwner, Holder, HostMarina, Platform }
    public required string PayeeKind { get; set; }

    // Marina ID for SlipOwner/HostMarina, BillingAccount ID for Holder, null for Platform
    public Guid? PayeeId { get; set; }

    // Fraction 0–1; must sum to 1.0 across all entries
    public decimal Percent { get; set; }
}
