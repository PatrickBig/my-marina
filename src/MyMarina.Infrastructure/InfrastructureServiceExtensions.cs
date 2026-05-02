using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Redis.StackExchange;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyMarina.Application.Abstractions;
using MyMarina.Infrastructure.Email;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Messaging;
using MyMarina.Infrastructure.Persistence;
using MyMarina.Infrastructure.Services;
using MyMarina.Infrastructure.UserContext;

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
                npgsql => npgsql
                    .MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                    .MigrationsHistoryTable("__EFMigrationsHistory", "mymarina"));
        });

        // --- Email ---
        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        var emailProvider = configuration.GetValue<string>("Email:Provider") ?? "null";
        if (string.Equals(emailProvider, "smtp2go", StringComparison.OrdinalIgnoreCase))
        {
            var host        = configuration.GetValue<string>("Email:Host");
            var username    = configuration.GetValue<string>("Email:Username");
            var password    = configuration.GetValue<string>("Email:Password");
            var fromAddress = configuration.GetValue<string>("Email:FromAddress");
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fromAddress))
                throw new InvalidOperationException(
                    "Email:Provider is 'smtp2go' but Host, Username, Password, FromAddress are required.");
            services.AddScoped<IEmailService, QueuedEmailService>();
            services.AddScoped<IMessageHandler<SendEmailMessage>, SmtpEmailMessageHandler>();
        }
        else
        {
            services.AddScoped<IEmailService, NullEmailService>();
        }

        // --- ASP.NET Core Identity ---
        var requireConfirmedEmail = configuration.GetValue<bool>("Email:RequireConfirmedEmail");
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = requireConfirmedEmail;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // --- User context (replaces v0 multi-tenancy) ---
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, HttpUserContext>();

        // --- Hangfire storage (feature flag: Hangfire:UseRedis) ---
        services.AddHangfire((sp, config) =>
        {
            var conf     = sp.GetRequiredService<IConfiguration>();
            var useRedis = conf.GetValue<bool>("Hangfire:UseRedis", defaultValue: true);

            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                  .UseSimpleAssemblyNameTypeSerializer()
                  .UseRecommendedSerializerSettings();

            if (useRedis)
            {
                var redisCs = conf.GetConnectionString("Redis")
                    ?? throw new InvalidOperationException("Redis connection string required when Hangfire:UseRedis=true.");
                config.UseRedisStorage(redisCs);
            }
            else
            {
                var pgCs = conf.GetConnectionString("Postgres")
                    ?? throw new InvalidOperationException("Postgres connection string required when Hangfire:UseRedis=false.");
                config.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(pgCs));
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
