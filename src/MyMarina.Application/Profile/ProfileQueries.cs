using MyMarina.Application.Abstractions;

namespace MyMarina.Application.Profile;

public sealed record GetProfileQuery;

public sealed record GetProfileResult(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber);

public interface IGetProfileQueryHandler : IQueryHandler<GetProfileQuery, GetProfileResult>;
