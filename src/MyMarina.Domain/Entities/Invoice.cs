using MyMarina.Domain.Common;
using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

/// <summary>
/// A billing record issued to a CustomerAccount.
/// </summary>
public class Invoice : TenantEntity
{
    public Guid CustomerAccountId { get; init; }

    /// <summary>Human-readable invoice number, sequential per tenant.</summary>
    public required string InvoiceNumber { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateOnly IssuedDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue => TotalAmount - AmountPaid;
    public string? Notes { get; set; }

    public CustomerAccount CustomerAccount { get; init; } = null!;
    public ICollection<InvoiceLineItem> LineItems { get; init; } = [];
    public ICollection<Payment> Payments { get; init; } = [];
}
