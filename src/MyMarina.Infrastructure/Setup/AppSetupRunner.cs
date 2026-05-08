using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarina.Infrastructure.Demo;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Setup;

public class AppSetupRunner(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IOptions<SetupOptions> options,
    DemoSeedScript demoSeed,
    ILogger<AppSetupRunner> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Running database migrations");
        await db.Database.MigrateAsync(ct);
        logger.LogInformation("Migrations complete");

        await EnsurePlatformOperatorAsync(ct);
        await EnsureInitialMarinaOwnerAsync(ct);
        await demoSeed.SeedAsync(ct);

        logger.LogInformation("Setup complete");
    }

    private async Task EnsurePlatformOperatorAsync(CancellationToken ct)
    {
        var opts = options.Value.PlatformOperator;
        if (string.IsNullOrWhiteSpace(opts.Email)) return;

        if (!await roleManager.RoleExistsAsync("PlatformOperator"))
            await roleManager.CreateAsync(new ApplicationRole("PlatformOperator"));

        if (await userManager.FindByEmailAsync(opts.Email) is not null)
        {
            logger.LogInformation("Platform operator {Email} already exists", opts.Email);
            return;
        }

        logger.LogInformation("Creating platform operator {Email}", opts.Email);
        var user = new ApplicationUser
        {
            Id             = Guid.CreateVersion7(),
            UserName       = opts.Email,
            Email          = opts.Email,
            FirstName      = "Platform",
            LastName       = "Operator",
            EmailConfirmed = true,
        };
        var result = await userManager.CreateAsync(user, opts.Password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, "PlatformOperator");
        else
            logger.LogError("Failed to create platform operator: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    private async Task EnsureInitialMarinaOwnerAsync(CancellationToken ct)
    {
        var opts = options.Value.InitialMarina;
        if (string.IsNullOrWhiteSpace(opts.OwnerEmail)) return;

        if (await userManager.FindByEmailAsync(opts.OwnerEmail) is not null)
        {
            logger.LogInformation("Marina owner {Email} already exists", opts.OwnerEmail);
            return;
        }

        logger.LogInformation("Creating initial marina owner {Email}", opts.OwnerEmail);
        var owner = new ApplicationUser
        {
            Id             = Guid.CreateVersion7(),
            UserName       = opts.OwnerEmail,
            Email          = opts.OwnerEmail,
            FirstName      = "Marina",
            LastName       = "Owner",
            EmailConfirmed = true,
        };
        var result = await userManager.CreateAsync(owner, opts.OwnerPassword);
        if (!result.Succeeded)
            logger.LogError("Failed to create marina owner: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}
