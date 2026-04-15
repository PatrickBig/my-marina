using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace MyMarina.IntegrationTests;

/// <summary>
/// Spins up a real Postgres container for integration tests.
/// Each test class that uses IClassFixture&lt;ApiWebApplicationFactory&gt; gets an isolated database.
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Credentials for the seeded platform operator — available to all test classes.
    public const string PlatformOperatorEmail    = "admin@mymarina.org";
    public const string PlatformOperatorPassword = "Admin@Marina123!";
    public Guid         PlatformOperatorId       { get; private set; }

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("mymarina_test")
        .WithUsername("mymarina")
        .WithPassword("mymarina")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Trigger WebApplicationFactory initialization so Services is available
        _ = CreateClient();

        // Seed a platform operator so Auth tests can call POST /auth/login
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var existing = await userManager.FindByEmailAsync(PlatformOperatorEmail);
        if (existing is null)
        {
            var platformOp = new ApplicationUser
            {
                UserName  = PlatformOperatorEmail,
                Email     = PlatformOperatorEmail,
                FirstName = "Platform",
                LastName  = "Admin",
                Role      = UserRole.PlatformOperator,
            };
            var result = await userManager.CreateAsync(platformOp, PlatformOperatorPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Seeding platform operator failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            PlatformOperatorId = platformOp.Id;
        }
        else
        {
            PlatformOperatorId = existing.Id;
        }
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use PostgreSQL for Hangfire storage in tests — avoids needing a Redis container.
        // Override ConnectionStrings:Postgres so Hangfire (and the health check) use the
        // Testcontainers dynamic port instead of the hardcoded localhost:5432 in appsettings.json.
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:UseRedis"]          = "false",
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Redis"]    = "localhost:6379,abortConnect=false"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace the production DbContext with one pointing at the test container
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            // Migrate synchronously at startup
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        });
    }

    /// <summary>Returns an HttpClient with a Bearer token pre-applied.</summary>
    public HttpClient CreateClientWithToken(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public HttpClient CreatePlatformOperatorClient()
        => CreateClientWithToken(TestJwtHelper.PlatformOperatorToken());

    public HttpClient CreateMarinaOwnerClient(Guid tenantId, Guid? marinaId = null)
        => CreateClientWithToken(TestJwtHelper.MarinaOwnerToken(tenantId, marinaId));
}
