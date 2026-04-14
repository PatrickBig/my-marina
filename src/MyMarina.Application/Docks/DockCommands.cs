using MyMarina.Application.Abstractions;

namespace MyMarina.Application.Docks;

public sealed record CreateDockCommand(
    Guid MarinaId,
    string Name,
    string? Description,
    int SortOrder = 0);

public sealed record UpdateDockCommand(
    Guid DockId,
    string Name,
    string? Description,
    int SortOrder);

public sealed record DeleteDockCommand(Guid DockId);

public interface ICreateDockCommandHandler : ICommandHandler<CreateDockCommand, Guid>;
public interface IUpdateDockCommandHandler : ICommandHandler<UpdateDockCommand>;
public interface IDeleteDockCommandHandler : ICommandHandler<DeleteDockCommand>;
