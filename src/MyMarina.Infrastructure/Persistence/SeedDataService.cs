using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Domain.Common;
using MyMarina.Domain.Entities;
using MyMarina.Domain.ValueObjects;
using MyMarina.Infrastructure.Identity;

namespace MyMarina.Infrastructure.Persistence;

/// <summary>
/// Provides test data seeding for development and testing.
/// This is NOT production code — seed data is for local dev/testing only.
/// </summary>
public class SeedDataService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager)
{
    private async Task<Guid> RoleIdAsync(string roleName)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName)
            ?? throw new InvalidOperationException($"Role '{roleName}' not found.");
        return role.Id;
    }

    public async Task SeedAsync()
    {
        await SeedPlatformAdminAsync();
        await SeedTenantWithMarinaAsync();
    }

    private async Task SeedPlatformAdminAsync()
    {
        const string email = "admin@mymarina.org";
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                Email = email,
                UserName = email,
                FirstName = "Platform",
                LastName = "Admin",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            var result = await userManager.CreateAsync(user, "TempPassword123!");
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to create platform admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        var platformAdminRoleId = await RoleIdAsync(Roles.PlatformAdmin);

        var existingContext = await db.UserContexts.FirstOrDefaultAsync(
            uc => uc.UserId == user.Id && uc.RoleId == platformAdminRoleId);

        if (existingContext == null)
        {
            db.UserContexts.Add(new UserContext
            {
                Id = Guid.CreateVersion7(),
                UserId = user.Id,
                RoleId = platformAdminRoleId,
                TenantId = Guid.Empty,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }
    }

    private async Task SeedTenantWithMarinaAsync()
    {
        var existingTenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == "test-marina");
        Guid tenantId;
        Guid marinaId;

        if (existingTenant == null)
        {
            var tenant = new Tenant
            {
                Id = Guid.CreateVersion7(),
                Name = "Test Marina",
                Slug = "test-marina",
                SubscriptionTier = Domain.Enums.SubscriptionTier.Pro,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
            tenantId = tenant.Id;
        }
        else
        {
            tenantId = existingTenant.Id;
        }

        // Check for the specific test marina
        var marina = await db.Marinas.FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Name == "Test Marina Location");
        if (marina == null)
        {
            marina = new Marina
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenantId,
                Name = "Test Marina Location",
                Address = new Address(
                    Street: "123 Dock Street",
                    City: "Coastal Town",
                    State: "CA",
                    Zip: "90001",
                    Country: "USA"),
                PhoneNumber = "555-0100",
                Email = "marina@mymarina.org",
                TimeZoneId = "America/New_York",
                CreatedAt = DateTimeOffset.UtcNow,
            };

            db.Marinas.Add(marina);
            await db.SaveChangesAsync();
        }
        marinaId = marina.Id;

        // Tenant Owner
        await SeedUserWithContextAsync(
            "owner@mymarina.org",
            "Marina",
            "Owner",
            await RoleIdAsync(Roles.TenantOwner),
            tenantId,
            null,
            null);

        // Marina Manager
        await SeedUserWithContextAsync(
            "manager@mymarina.org",
            "Marina",
            "Manager",
            await RoleIdAsync(Roles.MarinaManager),
            tenantId,
            marinaId,
            null);

        // Marina Staff
        await SeedUserWithContextAsync(
            "staff@mymarina.org",
            "Marina",
            "Staff",
            await RoleIdAsync(Roles.MarinaStaff),
            tenantId,
            marinaId,
            null);

        // Customer
        var customerAccount = await db.CustomerAccounts.FirstOrDefaultAsync(c => c.BillingEmail == "customer@mymarina.org");
        Guid customerAccountId;

        if (customerAccount == null)
        {
            customerAccount = new CustomerAccount
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenantId,
                DisplayName = "Test Customer",
                BillingEmail = "customer@mymarina.org",
                CreatedAt = DateTimeOffset.UtcNow,
            };

            db.CustomerAccounts.Add(customerAccount);
            await db.SaveChangesAsync();
            customerAccountId = customerAccount.Id;
        }
        else
        {
            customerAccountId = customerAccount.Id;
        }

        // Create customer user and context
        var customerUser = await userManager.FindByEmailAsync("customer@mymarina.org");
        if (customerUser == null)
        {
            customerUser = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                Email = "customer@mymarina.org",
                UserName = "customer@mymarina.org",
                FirstName = "Test",
                LastName = "Customer",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            var result = await userManager.CreateAsync(customerUser, "TempPassword123!");
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to create customer user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            var customerContext = new UserContext
            {
                Id = Guid.CreateVersion7(),
                UserId = customerUser.Id,
                RoleId = await RoleIdAsync(Roles.Customer),
                TenantId = tenantId,
                MarinaId = null,
                CustomerAccountId = customerAccountId,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            db.UserContexts.Add(customerContext);
            await db.SaveChangesAsync();

            // Create CustomerAccountMember linking user to account
            var existingMember = await db.CustomerAccountMembers.FirstOrDefaultAsync(
                m => m.CustomerAccountId == customerAccountId && m.UserId == customerUser.Id);
            if (existingMember == null)
            {
                var member = new CustomerAccountMember
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = tenantId,
                    CustomerAccountId = customerAccountId,
                    UserId = customerUser.Id,
                    Role = Domain.Enums.CustomerAccountMemberRole.Owner,
                    CreatedAt = DateTimeOffset.UtcNow,
                };

                db.CustomerAccountMembers.Add(member);
                await db.SaveChangesAsync();
            }
        }
    }

    private async Task SeedUserWithContextAsync(
        string email,
        string firstName,
        string lastName,
        Guid roleId,
        Guid tenantId,
        Guid? marinaId,
        Guid? customerAccountId)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                Email = email,
                UserName = email,
                FirstName = firstName,
                LastName = lastName,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            var result = await userManager.CreateAsync(user, "TempPassword123!");
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Always ensure a UserContext exists for this user/tenant/role combination
        var existingContext = await db.UserContexts.FirstOrDefaultAsync(
            uc => uc.UserId == user.Id && uc.TenantId == tenantId && uc.RoleId == roleId);

        if (existingContext == null)
        {
            var context = new UserContext
            {
                Id = Guid.CreateVersion7(),
                UserId = user.Id,
                RoleId = roleId,
                TenantId = tenantId,
                MarinaId = marinaId,
                CustomerAccountId = customerAccountId,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            db.UserContexts.Add(context);
            await db.SaveChangesAsync();
        }
    }
}
