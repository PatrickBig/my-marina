using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarina.Domain.Common;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Jobs;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Setup;

/// <summary>
/// Hosted service that runs when the application is started with the <c>--setup</c> argument.
/// Intended to run on every deployment as a Kubernetes pre-install/pre-upgrade Job or Helm hook.
/// <list type="bullet">
///   <item>Applies pending EF Core migrations</item>
///   <item>Seeds the platform-operator account (idempotent)</item>
///   <item>Seeds an initial marina tenant (idempotent, optional)</item>
///   <item>Registers/updates all Hangfire recurring jobs in storage</item>
/// </list>
/// Exits with code 0 on success, 1 on failure.
/// </summary>
public sealed class SetupHostedService(
    IServiceScopeFactory scopeFactory,
    IRecurringJobManager recurringJobs,
    IOptions<SetupOptions> options,
    ILogger<SetupHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("=== MyMarina Setup starting ===");

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await ApplyMigrationsAsync(db, cancellationToken);
            await SeedPlatformOperatorAsync(db, userManager, cancellationToken);
            await SeedInitialMarinaAsync(db, userManager, cancellationToken);
            ConfigureRecurringJobs();

            logger.LogInformation("=== MyMarina Setup completed successfully ===");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "=== MyMarina Setup FAILED ===");
            Environment.Exit(1);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // ── Steps ─────────────────────────────────────────────────────────────────

    private void ConfigureRecurringJobs()
    {
        logger.LogInformation("Configuring recurring jobs…");

        recurringJobs.AddOrUpdate<MarkOverdueInvoicesJob>(
            "mark-overdue-invoices",
            job => job.ExecuteAsync(),
            Cron.Daily);

        logger.LogInformation("Recurring jobs configured.");
    }

    private async Task ApplyMigrationsAsync(AppDbContext db, CancellationToken ct)
    {
        logger.LogInformation("Applying database migrations…");
        var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();

        if (pending.Count == 0)
        {
            logger.LogInformation("No pending migrations.");
            return;
        }

        logger.LogInformation("Applying {Count} migration(s): {Migrations}",
            pending.Count, string.Join(", ", pending));

        await db.Database.MigrateAsync(ct);
        logger.LogInformation("Migrations applied.");
    }

    private static async Task<Guid> RequireRoleIdAsync(AppDbContext db, string roleName, CancellationToken ct)
    {
        var role = await db.AuthorizationRoles.FirstOrDefaultAsync(r => r.Name == roleName, ct)
            ?? throw new InvalidOperationException($"Role '{roleName}' not found. Ensure migrations have run.");
        return role.Id;
    }

    private async Task SeedPlatformOperatorAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        CancellationToken ct)
    {
        var cfg = options.Value.PlatformOperator;
        if (cfg is null)
        {
            logger.LogInformation("No PlatformOperator config — skipping platform operator seed.");
            return;
        }

        if (string.IsNullOrWhiteSpace(cfg.Email))
            throw new InvalidOperationException("Setup:PlatformOperator:Email is required.");
        if (string.IsNullOrWhiteSpace(cfg.Password))
            throw new InvalidOperationException("Setup:PlatformOperator:Password is required.");

        var existing = await userManager.FindByEmailAsync(cfg.Email);
        if (existing is null)
        {
            logger.LogInformation("Creating platform operator '{Email}'…", cfg.Email);

            var user = new ApplicationUser
            {
                UserName  = cfg.Email,
                Email     = cfg.Email,
                FirstName = cfg.FirstName,
                LastName  = cfg.LastName,
                Role      = UserRole.PlatformOperator,
            };

            var result = await userManager.CreateAsync(user, cfg.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create platform operator: {errors}");
            }

            existing = user;
            logger.LogInformation("Platform operator '{Email}' created (id={Id}).", cfg.Email, user.Id);
        }
        else
        {
            logger.LogInformation("Platform operator '{Email}' already exists — skipping user creation.", cfg.Email);
        }

        var platformAdminRoleId = await RequireRoleIdAsync(db, Roles.PlatformAdmin, ct);

        var existingContext = await db.UserContexts
            .FirstOrDefaultAsync(uc => uc.UserId == existing.Id && uc.RoleId == platformAdminRoleId, ct);

        if (existingContext is null)
        {
            db.UserContexts.Add(new UserContext
            {
                UserId   = existing.Id,
                RoleId   = platformAdminRoleId,
                TenantId = Guid.Empty,
            });
            await db.SaveChangesAsync(ct);
            logger.LogInformation("UserContext created for platform operator '{Email}'.", cfg.Email);
        }
    }

    private async Task SeedInitialMarinaAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        CancellationToken ct)
    {
        var cfg = options.Value.InitialMarina;
        if (cfg is null)
        {
            logger.LogInformation("No InitialMarina config — skipping initial marina seed.");
            return;
        }

        if (string.IsNullOrWhiteSpace(cfg.TenantName))
            throw new InvalidOperationException("Setup:InitialMarina:TenantName is required.");
        if (string.IsNullOrWhiteSpace(cfg.TenantSlug))
            throw new InvalidOperationException("Setup:InitialMarina:TenantSlug is required.");
        if (string.IsNullOrWhiteSpace(cfg.OwnerEmail))
            throw new InvalidOperationException("Setup:InitialMarina:OwnerEmail is required.");
        if (string.IsNullOrWhiteSpace(cfg.OwnerPassword))
            throw new InvalidOperationException("Setup:InitialMarina:OwnerPassword is required.");

        // Tenant — idempotent on slug
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == cfg.TenantSlug, ct);
        if (tenant is null)
        {
            logger.LogInformation("Creating tenant '{Slug}'…", cfg.TenantSlug);
            tenant = new Tenant
            {
                Name             = cfg.TenantName,
                Slug             = cfg.TenantSlug,
                SubscriptionTier = SubscriptionTier.Free,
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Tenant '{Slug}' created (id={Id}).", cfg.TenantSlug, tenant.Id);
        }
        else
        {
            logger.LogInformation("Tenant '{Slug}' already exists — skipping tenant creation.", cfg.TenantSlug);
        }

        // Marina owner — idempotent on email
        var existingOwner = await userManager.FindByEmailAsync(cfg.OwnerEmail);
        if (existingOwner is null)
        {
            logger.LogInformation("Creating marina owner '{Email}'…", cfg.OwnerEmail);

            var owner = new ApplicationUser
            {
                UserName  = cfg.OwnerEmail,
                Email     = cfg.OwnerEmail,
                FirstName = cfg.OwnerFirstName,
                LastName  = cfg.OwnerLastName,
                Role      = UserRole.MarinaOwner,
                TenantId  = tenant.Id,
            };

            var result = await userManager.CreateAsync(owner, cfg.OwnerPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create marina owner: {errors}");
            }

            existingOwner = owner;
            logger.LogInformation("Marina owner '{Email}' created (id={Id}).", cfg.OwnerEmail, owner.Id);
        }
        else
        {
            logger.LogInformation("Marina owner '{Email}' already exists — skipping user creation.", cfg.OwnerEmail);
        }

        var tenantOwnerRoleId = await RequireRoleIdAsync(db, Roles.TenantOwner, ct);

        var existingOwnerContext = await db.UserContexts
            .FirstOrDefaultAsync(uc => uc.UserId == existingOwner.Id && uc.RoleId == tenantOwnerRoleId, ct);

        if (existingOwnerContext is null)
        {
            db.UserContexts.Add(new UserContext
            {
                UserId   = existingOwner.Id,
                RoleId   = tenantOwnerRoleId,
                TenantId = tenant.Id,
            });
            await db.SaveChangesAsync(ct);
            logger.LogInformation("UserContext created for marina owner '{Email}'.", cfg.OwnerEmail);
        }
    }
}
