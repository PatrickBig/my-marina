using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.Slips;

public sealed record GetSlipsQuery(Guid MarinaId);

/// <summary>
/// Returns slips that can physically fit a boat of the given dimensions
/// and have no active assignment overlapping the requested date range.
/// </summary>
public sealed record GetAvailableSlipsQuery(
    Guid MarinaId,
    decimal BoatLength,
    decimal BoatBeam,
    decimal BoatDraft,
    DateOnly StartDate,
    DateOnly? EndDate);

public sealed record SlipDto(
    Guid Id,
    Guid MarinaId,
    Guid? DockId,
    string Name,
    SlipType SlipType,
    decimal MaxLength,
    decimal MaxBeam,
    decimal MaxDraft,
    bool HasElectric,
    ElectricService? Electric,
    bool HasWater,
    RateType RateType,
    decimal? DailyRate,
    decimal? MonthlyRate,
    decimal? AnnualRate,
    SlipStatus Status,
    decimal? Latitude,
    decimal? Longitude,
    string? Notes,
    DateTimeOffset CreatedAt);

public interface IGetSlipsQueryHandler : IQueryHandler<GetSlipsQuery, IReadOnlyList<SlipDto>>;
public interface IGetAvailableSlipsQueryHandler : IQueryHandler<GetAvailableSlipsQuery, IReadOnlyList<SlipDto>>;
