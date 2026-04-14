using MyMarina.Domain.Common;
using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

/// <summary>
/// A vessel registered to a CustomerAccount. A CustomerAccount may have multiple boats.
/// </summary>
public class Boat : TenantEntity
{
    public Guid CustomerAccountId { get; init; }
    public required string Name { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public decimal Length { get; set; }
    public decimal Beam { get; set; }
    public decimal Draft { get; set; }
    public BoatType BoatType { get; set; }
    public string? HullColor { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? RegistrationState { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? InsurancePolicyNumber { get; set; }
    public DateOnly? InsuranceExpiresOn { get; set; }

    public CustomerAccount CustomerAccount { get; init; } = null!;
}
