using MyMarina.Domain.Common;
using MyMarina.Domain.ValueObjects;

namespace MyMarina.Domain.Entities;

/// <summary>
/// The billing and ownership entity for a marina customer. Multiple users
/// (family members, business partners, etc.) can belong to one account
/// via CustomerAccountMember.
/// </summary>
public class CustomerAccount : TenantEntity
{
    /// <summary>
    /// Display name for the account (e.g., "Smith Family", "Blue Water Charters LLC").
    /// </summary>
    public required string DisplayName { get; set; }

    public required string BillingEmail { get; set; }
    public string? BillingPhone { get; set; }
    public Address? BillingAddress { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }

    /// <summary>Internal marina notes — not visible to the customer.</summary>
    public string? Notes { get; set; }

    public ICollection<CustomerAccountMember> Members { get; init; } = [];
    public ICollection<Boat> Boats { get; init; } = [];
    public ICollection<Invoice> Invoices { get; init; } = [];
    public ICollection<SlipAssignment> SlipAssignments { get; init; } = [];
    public ICollection<MaintenanceRequest> MaintenanceRequests { get; init; } = [];
}
