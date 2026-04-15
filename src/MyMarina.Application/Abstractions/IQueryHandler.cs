namespace MyMarina.Application.Abstractions;

/// <summary>
/// Handles a query that returns a result.
/// </summary>
public interface IQueryHandler<TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
}
