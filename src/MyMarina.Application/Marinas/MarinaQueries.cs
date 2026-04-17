using MyMarina.Application.Abstractions;
using MyMarina.Application.Common;

namespace MyMarina.Application.Marinas;

public sealed record GetMarinasQuery;
public sealed record GetMarinaQuery(Guid MarinaId);
public sealed record GetMarinaHealthTargetsQuery(Guid MarinaId);
public sealed record GetMarinaMetricsQuery(Guid MarinaId);

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

public sealed record HealthTargetsDto(
    decimal? OccupancyRateTarget,
    int? OverdueARThresholdDays,
    decimal? TargetMonthlyRevenue);

public sealed record MarinaMetricsDto(
    int TotalSlips,
    int OccupiedSlips,
    decimal OccupancyRate,
    decimal OutstandingAR,
    int OldestOverdueDays,
    int ActiveCustomerCount,
    HealthStatus HealthStatus);

public interface IGetMarinasQueryHandler : IQueryHandler<GetMarinasQuery, IReadOnlyList<MarinaDto>>;
public interface IGetMarinaQueryHandler : IQueryHandler<GetMarinaQuery, MarinaDto?>;
public interface IGetMarinaHealthTargetsQueryHandler : IQueryHandler<GetMarinaHealthTargetsQuery, HealthTargetsDto?>;
public interface IGetMarinaMetricsQueryHandler : IQueryHandler<GetMarinaMetricsQuery, MarinaMetricsDto?>;
