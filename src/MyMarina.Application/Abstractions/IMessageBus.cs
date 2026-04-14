namespace MyMarina.Application.Abstractions;

/// <summary>
/// Decouples message producers from consumers. MVP: backed by Hangfire.
/// Future: swap to NATS JetStream via a DI registration change only.
/// </summary>
public interface IMessageBus
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
    Task ScheduleAsync<T>(T message, DateTimeOffset runAt, CancellationToken ct = default) where T : class;
}

/// <summary>
/// Handles a message published via IMessageBus.
/// Implementations are auto-discovered by assembly scanning.
/// </summary>
public interface IMessageHandler<T> where T : class
{
    Task HandleAsync(T message, CancellationToken ct = default);
}
