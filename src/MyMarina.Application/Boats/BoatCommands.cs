using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.Boats;

public sealed record CreateBoatCommand(
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
    DateOnly? InsuranceExpiresOn);

public sealed record UpdateBoatCommand(
    Guid BoatId,
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
    DateOnly? InsuranceExpiresOn);

public sealed record DeleteBoatCommand(Guid BoatId);

public interface ICreateBoatCommandHandler : ICommandHandler<CreateBoatCommand, Guid>;
public interface IUpdateBoatCommandHandler : ICommandHandler<UpdateBoatCommand>;
public interface IDeleteBoatCommandHandler : ICommandHandler<DeleteBoatCommand>;
