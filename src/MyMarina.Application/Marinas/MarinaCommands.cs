using MyMarina.Application.Abstractions;
using MyMarina.Application.Common;

namespace MyMarina.Application.Marinas;

public sealed record CreateMarinaCommand(
    string Name,
    AddressDto Address,
    string PhoneNumber,
    string Email,
    string TimeZoneId,
    string? Website,
    string? Description);

public sealed record UpdateMarinaCommand(
    Guid MarinaId,
    string Name,
    AddressDto Address,
    string PhoneNumber,
    string Email,
    string TimeZoneId,
    string? Website,
    string? Description);

public sealed record UpdateMarinaHealthTargetsCommand(
    Guid MarinaId,
    HealthTargetsDto HealthTargets);

public interface ICreateMarinaCommandHandler : ICommandHandler<CreateMarinaCommand, Guid>;
public interface IUpdateMarinaCommandHandler : ICommandHandler<UpdateMarinaCommand>;
public interface IUpdateMarinaHealthTargetsCommandHandler : ICommandHandler<UpdateMarinaHealthTargetsCommand>;
