using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.Boats;

public sealed record GetBoatsQuery(Guid CustomerAccountId);

public sealed record BoatDto(
    Guid Id,
    Guid CustomerAccountId,
    string Name,
    string? Make,
    string? Model,
    int? Year,
    decimal Length,
    decimal Beam,
    decimal Draft,
    BoatType BoatType,
    string? HullColor,
    string? RegistrationNumber,
    string? RegistrationState,
    string? InsuranceProvider,
    string? InsurancePolicyNumber,
    DateOnly? InsuranceExpiresOn,
    DateTimeOffset CreatedAt);

public interface IGetBoatsQueryHandler : IQueryHandler<GetBoatsQuery, IReadOnlyList<BoatDto>>;
