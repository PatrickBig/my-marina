using Microsoft.Extensions.DependencyInjection;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Demo;

namespace MyMarina.Infrastructure.Demo;

/// <summary>
/// Hangfire job wrapper for DeleteExpiredDemoTenantsCommand.
/// Registered as a recurring job every 15 minutes by SetupHostedService.
/// </summary>
public class DeleteExpiredDemoTenantsJob(IServiceScopeFactory scopeFactory)
{
    public async Task ExecuteAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<DeleteExpiredDemoTenantsCommand>>();
        await handler.HandleAsync(new DeleteExpiredDemoTenantsCommand());
    }
}
