using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.Portal;

public sealed record SubmitMaintenanceRequestCommand(
    string Title,
    string Description,
    Priority Priority,
    Guid? SlipId,
    Guid? BoatId);

public interface ISubmitMaintenanceRequestCommandHandler
    : ICommandHandler<SubmitMaintenanceRequestCommand, Guid>;
