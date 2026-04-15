using MyMarina.Application.Abstractions;
using MyMarina.Application.Common;

namespace MyMarina.Application.Marinas;

public sealed record GetMarinasQuery;
public sealed record GetMarinaQuery(Guid MarinaId);

public sealed record MarinaDto(
    Guid Id,
    Guid TenantId,
    string Name,
    AddressDto Address,
    string PhoneNumber,
    string Email,
    string TimeZoneId,
    string? Website,
    string? Description,
    DateTimeOffset CreatedAt);

public interface IGetMarinasQueryHandler : IQueryHandler<GetMarinasQuery, IReadOnlyList<MarinaDto>>;
public interface IGetMarinaQueryHandler : IQueryHandler<GetMarinaQuery, MarinaDto?>;
