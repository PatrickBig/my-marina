using Hangfire;
using MyMarina.Application.Abstractions;

namespace MyMarina.Infrastructure.Messaging;

/// <summary>
/// MVP IMessageBus implementation backed by Hangfire + Redis.
/// Swap this registration for NatsMessageBus when streaming is needed —
/// zero application code changes required.
/// </summary>
public class HangfireMessageBus(IBackgroundJobClient jobClient) : IMessageBus
{
    public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
    {
        jobClient.Enqueue<IMessageHandler<T>>(h => h.HandleAsync(message, CancellationToken.None));
        return Task.CompletedTask;
    }

    public Task ScheduleAsync<T>(T message, DateTimeOffset runAt, CancellationToken ct = default) where T : class
    {
        var delay = runAt - DateTimeOffset.UtcNow;
        jobClient.Schedule<IMessageHandler<T>>(h => h.HandleAsync(message, CancellationToken.None), delay);
        return Task.CompletedTask;
    }
}
