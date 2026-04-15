namespace MyMarina.Application.Abstractions;

/// <summary>
/// Provides the currently active marina for the request.
/// Null for corporate operators and customers (not locked to one marina).
/// </summary>
public interface IMarinaContext
{
    Guid? MarinaId { get; }
}
