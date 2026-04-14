using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.Slips;

public sealed record CreateSlipCommand(
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
    string? Notes);

public sealed record UpdateSlipCommand(
    Guid SlipId,
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
    string? Notes);

public sealed record DeleteSlipCommand(Guid SlipId);

public interface ICreateSlipCommandHandler : ICommandHandler<CreateSlipCommand, Guid>;
public interface IUpdateSlipCommandHandler : ICommandHandler<UpdateSlipCommand>;
public interface IDeleteSlipCommandHandler : ICommandHandler<DeleteSlipCommand>;
