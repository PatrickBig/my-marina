namespace MyMarina.Application.Abstractions;

/// <summary>
/// Handles a command that produces no result.
/// Cross-cutting concerns (logging, validation, authorization) are
/// applied as Scrutor decorators — no pipeline magic.
/// </summary>
public interface ICommandHandler<TCommand>
{
    Task HandleAsync(TCommand command, CancellationToken ct = default);
}

/// <summary>
/// Handles a command that produces a result.
/// </summary>
public interface ICommandHandler<TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default);
}
