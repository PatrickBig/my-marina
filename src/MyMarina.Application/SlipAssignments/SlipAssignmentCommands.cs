using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.SlipAssignments;

public sealed record CreateSlipAssignmentCommand(
    Guid SlipId,
    Guid CustomerAccountId,
    Guid BoatId,
    AssignmentType AssignmentType,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal? RateOverride,
    string? Notes);

public sealed record EndSlipAssignmentCommand(
    Guid SlipAssignmentId,
    DateOnly EndDate);

public interface ICreateSlipAssignmentCommandHandler : ICommandHandler<CreateSlipAssignmentCommand, Guid>;
public interface IEndSlipAssignmentCommandHandler : ICommandHandler<EndSlipAssignmentCommand>;
