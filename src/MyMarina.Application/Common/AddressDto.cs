namespace MyMarina.Application.Common;

/// <summary>
/// Shared address DTO used across feature commands and query results.
/// Maps to and from the Address value object in the Domain.
/// </summary>
public sealed record AddressDto(
    string Street,
    string City,
    string State,
    string Zip,
    string Country);
