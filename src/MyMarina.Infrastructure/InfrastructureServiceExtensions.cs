using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Redis.StackExchange;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyMarina.Application.Abstractions;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Messaging;
using MyMarina.Infrastructure.MultiTenancy;
using MyMarina.Infrastructure.Persistence;
using MyMarina.Infrastructure.Services;

namespace MyMarina.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool registerHangfireServer = true)
    {
        // --- EF Core + Postgres ---
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
        });

        // --- ASP.NET Core Identity ---
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // --- Multi-tenancy ---
        services.AddHttpContextAccessor();
        services.AddScoped<HttpTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<HttpTenantContext>());
        services.AddScoped<IMarinaContext>(sp => sp.GetRequiredService<HttpTenantContext>());
        services.AddScoped<ICustomerContext>(sp => sp.GetRequiredService<HttpTenantContext>());

        // --- Hangfire storage (feature flag: Hangfire:UseRedis) ---
        // Configuration is read lazily inside the callback so that test-time overrides
        // (e.g. setting Hangfire:UseRedis=false via WebApplicationFactory) take effect
        // before the storage is initialised.
        services.AddHangfire((sp, config) =>
        {
            var conf     = sp.GetRequiredService<IConfiguration>();
            var useRedis = conf.GetValue<bool>("Hangfire:UseRedis", defaultValue: true);

            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                  .UseSimpleAssemblyNameTypeSerializer()
                  .UseRecommendedSerializerSettings();

            if (useRedis)
            {
                var redisConnectionString = conf.GetConnectionString("Redis")
                    ?? throw new InvalidOperationException("Redis connection string is required when Hangfire:UseRedis is true.");
                config.UseRedisStorage(redisConnectionString);
            }
            else
            {
                var pgConnection = conf.GetConnectionString("Postgres")
                    ?? throw new InvalidOperationException("Postgres connection string is required when Hangfire:UseRedis is false.");
                config.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(pgConnection));
            }
        });

        if (registerHangfireServer)
            services.AddHangfireServer();

        // --- Message bus ---
        services.AddScoped<IMessageBus, HangfireMessageBus>();

        // --- JWT token service ---
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // --- Command and query handlers (auto-registered via Scrutor) ---
        services.Scan(scan => scan
            .FromAssemblyOf<JwtTokenService>()
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        return services;
    }
}
