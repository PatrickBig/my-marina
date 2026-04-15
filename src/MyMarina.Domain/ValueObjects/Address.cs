namespace MyMarina.Domain.ValueObjects;

/// <summary>
/// Immutable address value object. Stored as owned entity by EF Core.
/// </summary>
public sealed record Address(
    string Street,
    string City,
    string State,
    string Zip,
    string Country);
