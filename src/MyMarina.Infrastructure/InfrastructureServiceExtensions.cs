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
        IConfiguration configuration)
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

        // --- Hangfire storage (feature flag: Hangfire:UseRedis) ---
        // Default is true (Redis). Set to false to use PostgreSQL storage instead
        // when Redis is not available (e.g. cost reduction, simpler infra).
        var useRedis = configuration.GetValue<bool>("Hangfire:UseRedis", defaultValue: true);
        if (useRedis)
        {
            var redisConnectionString = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Redis connection string is required when Hangfire:UseRedis is true.");
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseRedisStorage(redisConnectionString));
        }
        else
        {
            var pgConnection = configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Postgres connection string is required when Hangfire:UseRedis is false.");
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(pgConnection)));
        }

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
