using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MyMarina.Application.Abstractions;
using MyMarina.Infrastructure.Email;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;
using MyMarina.Infrastructure.Storage;
using Testcontainers.PostgreSql;

namespace MyMarina.IntegrationTests;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string PlatformOperatorEmail    = "admin@mymarina.org";
    public const string PlatformOperatorPassword = "Admin@Marina123!";
    public Guid PlatformOperatorId { get; private set; }

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("mymarina_test")
        .WithUsername("mymarina")
        .WithPassword("mymarina")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _ = CreateClient();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        if (!await roleManager.RoleExistsAsync("PlatformOperator"))
            await roleManager.CreateAsync(new ApplicationRole("PlatformOperator"));

        var existing = await userManager.FindByEmailAsync(PlatformOperatorEmail);
        if (existing is null)
        {
            var platformOp = new ApplicationUser
            {
                Id             = Guid.CreateVersion7(),
                UserName       = PlatformOperatorEmail,
                Email          = PlatformOperatorEmail,
                EmailConfirmed = true,
                FirstName      = "Platform",
                LastName       = "Admin",
            };
            var result = await userManager.CreateAsync(platformOp, PlatformOperatorPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Seeding platform operator failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            await userManager.AddToRoleAsync(platformOp, "PlatformOperator");
            PlatformOperatorId = platformOp.Id;
        }
        else
        {
            PlatformOperatorId = existing.Id;
        }
    }

    public new async Task DisposeAsync() => await _postgres.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:UseRedis"]           = "false",
                ["ConnectionStrings:Postgres"]  = _postgres.GetConnectionString(),
                ["ConnectionStrings:Redis"]     = "localhost:6379,abortConnect=false",
                ["Email:RequireConfirmedEmail"]  = "false",
                // Storage:Provider must be "S3" so InfrastructureServiceExtensions registers
                // IStorageProvider, but we immediately replace it with InMemoryStorageProvider below.
                ["Storage:Provider"]            = "S3",
                ["Storage:S3:Endpoint"]         = "http://localhost:4566",
                ["Storage:S3:Bucket"]           = "mymarina-test",
                ["Storage:S3:AccessKey"]        = "test",
                ["Storage:S3:SecretKey"]        = "test",
                ["Storage:S3:BucketPublicBaseUrl"] = "http://localhost/test-storage",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddScoped<IEmailService, NullEmailService>();

            // Replace S3StorageProvider with the in-memory test double.
            services.RemoveAll<IStorageProvider>();
            services.AddSingleton<IStorageProvider, InMemoryStorageProvider>();

            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });
    }

    public HttpClient CreateClientWithToken(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public HttpClient CreatePlatformOperatorClient()
        => CreateClientWithToken(TestJwtHelper.PlatformOperatorToken(PlatformOperatorId));
}
