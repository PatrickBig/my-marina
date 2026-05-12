using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace MyMarina.IntegrationTests;

[Collection("Integration")]
public class MarinaOnboardingWizardTests(ApiWebApplicationFactory factory)
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    async Task<(HttpClient client, Guid userId, Guid tenantId, Guid marinaId)> CreateDraftMarinaAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var userId = Guid.CreateVersion7();
        var email  = $"wizard-{userId:N}@example.com";

        var appUser = new ApplicationUser
        {
            Id = userId, UserName = email, Email = email,
            EmailConfirmed = true, FirstName = "Wizard", LastName = "Test",
        };
        await userManager.CreateAsync(appUser, "WizardTest!123");

        var tenant = new Domain.Entities.Tenant { Name = "Wizard Tenant", Slug = $"wizard-{userId:N}" };
        var marina = new Domain.Entities.Marina
        {
            TenantId = tenant.Id,
            Name     = "Draft Marina",
            Slug     = $"draft-{userId:N}",
            MarinaType = MarinaType.Commercial,
            IsSetupComplete = false,
        };
        var membership = new Domain.Entities.Membership
        {
            UserId    = userId,
            Scope     = MembershipScope.Marina,
            TenantId  = tenant.Id,
            MarinaId  = marina.Id,
            Role      = MembershipRole.Owner,
            AcceptedAt = DateTimeOffset.UtcNow,
        };

        db.Tenants.Add(tenant);
        db.Marinas.Add(marina);
        db.Memberships.Add(membership);
        await db.SaveChangesAsync();

        var token = TestJwtHelper.UserToken(userId, email,
            memberships: [new MembershipClaim(MembershipScope.Marina, tenant.Id, marina.Id, MembershipRole.Owner, null)]);
        var client = factory.CreateClientWithToken(token);

        return (client, userId, tenant.Id, marina.Id);
    }

    // ── PUT /marinas/{id}/setup/docks ─────────────────────────────────────────

    [Fact]
    public async Task SetupDocks_DraftMarina_Succeeds_And_ReplacesTree()
    {
        var (client, _, _, marinaId) = await CreateDraftMarinaAsync();

        var payload = new
        {
            docks = new[]
            {
                new
                {
                    name = "Dock A",
                    slips = new[]
                    {
                        new { name = "A-1", maxLength = 40m, maxBeam = 14m, maxDraft = 5m, slipType = "Floating", hasElectric = true, hasWater = true, hasPumpOut = false, isCovered = false, isIndoor = false, amenities = Array.Empty<string>() },
                        new { name = "A-2", maxLength = 35m, maxBeam = 12m, maxDraft = 4m, slipType = "Floating", hasElectric = false, hasWater = true, hasPumpOut = false, isCovered = false, isIndoor = false, amenities = Array.Empty<string>() },
                    }
                },
                new
                {
                    name = "Dock B",
                    slips = new[]
                    {
                        new { name = "B-1", maxLength = 50m, maxBeam = 16m, maxDraft = 6m, slipType = "Fixed", hasElectric = true, hasWater = true, hasPumpOut = true, isCovered = false, isIndoor = false, amenities = new[] { "Fuel dock" } },
                    }
                }
            }
        };

        var response = await client.PutAsJsonAsync($"/marinas/{marinaId}/setup/docks", payload);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the tree was created
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var docks = await db.Docks.Where(d => d.MarinaId == marinaId).ToListAsync();
        var slips = await db.Slips.Where(s => s.MarinaId == marinaId).ToListAsync();

        Assert.Equal(2, docks.Count);
        Assert.Equal(3, slips.Count);

        var fuelSlip = slips.Single(s => s.Name == "B-1");
        Assert.True(fuelSlip.HasPumpOut);
        Assert.Contains("Fuel dock", fuelSlip.Amenities);
    }

    [Fact]
    public async Task SetupDocks_ReplacesExistingTree()
    {
        var (client, _, _, marinaId) = await CreateDraftMarinaAsync();

        // First setup
        var first = new { docks = new[] { new { name = "Old Dock", slips = new[] { new { name = "O-1", maxLength = 30m, maxBeam = 10m, maxDraft = 4m, slipType = "Floating", hasElectric = false, hasWater = false, hasPumpOut = false, isCovered = false, isIndoor = false, amenities = Array.Empty<string>() } } } } };
        await client.PutAsJsonAsync($"/marinas/{marinaId}/setup/docks", first);

        // Second setup — should replace
        var second = new { docks = new[] { new { name = "New Dock", slips = new[] { new { name = "N-1", maxLength = 60m, maxBeam = 18m, maxDraft = 7m, slipType = "Fixed", hasElectric = true, hasWater = true, hasPumpOut = false, isCovered = false, isIndoor = false, amenities = Array.Empty<string>() } } } } };
        var response = await client.PutAsJsonAsync($"/marinas/{marinaId}/setup/docks", second);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var docks = await db.Docks.Where(d => d.MarinaId == marinaId).ToListAsync();

        Assert.Single(docks);
        Assert.Equal("New Dock", docks[0].Name);
        Assert.DoesNotContain(docks, d => d.Name == "Old Dock");
    }

    [Fact]
    public async Task SetupDocks_ActiveMarina_Returns409()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var userId = Guid.CreateVersion7();
        var email  = $"active-{userId:N}@example.com";
        var appUser = new ApplicationUser { Id = userId, UserName = email, Email = email, EmailConfirmed = true, FirstName = "A", LastName = "B" };
        await userManager.CreateAsync(appUser, "Active!Pass123");

        var tenant  = new Domain.Entities.Tenant { Name = "Active Tenant", Slug = $"active-{userId:N}" };
        var marina  = new Domain.Entities.Marina { TenantId = tenant.Id, Name = "Active Marina", Slug = $"active-m-{userId:N}", MarinaType = MarinaType.Commercial, IsSetupComplete = true };
        var membership = new Domain.Entities.Membership { UserId = userId, Scope = MembershipScope.Marina, TenantId = tenant.Id, MarinaId = marina.Id, Role = MembershipRole.Owner, AcceptedAt = DateTimeOffset.UtcNow };

        db.Tenants.Add(tenant); db.Marinas.Add(marina); db.Memberships.Add(membership);
        await db.SaveChangesAsync();

        var token  = TestJwtHelper.UserToken(userId, email, memberships: [new MembershipClaim(MembershipScope.Marina, tenant.Id, marina.Id, MembershipRole.Owner, null)]);
        var client = factory.CreateClientWithToken(token);

        var payload = new { docks = new[] { new { name = "D", slips = Array.Empty<object>() } } };
        var response = await client.PutAsJsonAsync($"/marinas/{marina.Id}/setup/docks", payload);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ── DELETE /marinas/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteDraftMarina_DraftMarina_Returns204()
    {
        var (client, _, _, marinaId) = await CreateDraftMarinaAsync();

        var response = await client.DeleteAsync($"/marinas/{marinaId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Null(await db.Marinas.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == marinaId));
    }

    [Fact]
    public async Task DeleteDraftMarina_ActiveMarina_Returns409()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var userId = Guid.CreateVersion7();
        var email  = $"nodelete-{userId:N}@example.com";
        var appUser = new ApplicationUser { Id = userId, UserName = email, Email = email, EmailConfirmed = true, FirstName = "X", LastName = "Y" };
        await userManager.CreateAsync(appUser, "NoDelete!Pass123");

        var tenant = new Domain.Entities.Tenant { Name = "No-Delete Tenant", Slug = $"nodelete-{userId:N}" };
        var marina = new Domain.Entities.Marina { TenantId = tenant.Id, Name = "Active Marina", Slug = $"nodelete-m-{userId:N}", MarinaType = MarinaType.Commercial, IsSetupComplete = true };
        var membership = new Domain.Entities.Membership { UserId = userId, Scope = MembershipScope.Marina, TenantId = tenant.Id, MarinaId = marina.Id, Role = MembershipRole.Owner, AcceptedAt = DateTimeOffset.UtcNow };

        db.Tenants.Add(tenant); db.Marinas.Add(marina); db.Memberships.Add(membership);
        await db.SaveChangesAsync();

        var token  = TestJwtHelper.UserToken(userId, email, memberships: [new MembershipClaim(MembershipScope.Marina, tenant.Id, marina.Id, MembershipRole.Owner, null)]);
        var client = factory.CreateClientWithToken(token);

        var response = await client.DeleteAsync($"/marinas/{marina.Id}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ── Draft marina excluded from marina rollup search ───────────────────────

    [Fact]
    public async Task SlipSearch_ExcludesDraftMarinaSlips()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenant = new Domain.Entities.Tenant { Name = "Draft Exclusion Tenant", Slug = $"draftex-{Guid.NewGuid():N}" };
        var marina = new Domain.Entities.Marina
        {
            TenantId = tenant.Id, Name = "Hidden Draft Marina",
            Slug = $"draftex-m-{Guid.NewGuid():N}", MarinaType = MarinaType.Commercial,
            IsSetupComplete = false,
            Latitude = 38.9784m, Longitude = -76.4922m,
        };
        var slip = new Domain.Entities.Slip
        {
            MarinaId = marina.Id, Name = "Draft Slip",
            MaxLength = 40, MaxBeam = 14, MaxDraft = 5,
            Status = SlipStatus.Active,
            DefaultTransientRateKind = Domain.Enums.RateKind.Flat, DefaultTransientBaseRate = 100m,
        };

        db.Tenants.Add(tenant); db.Marinas.Add(marina); db.Slips.Add(slip);
        await db.SaveChangesAsync();

        var client   = factory.CreateClient();
        // Use the new marina rollup endpoint — draft (IsSetupComplete=false) marinas must be excluded
        var response = await client.GetAsync("/marinas/search?north=39.0&south=38.9&east=-76.4&west=-76.6&listingKind=Transient");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<MarinaRollupResult[]>();
        Assert.NotNull(results);
        Assert.DoesNotContain(results, r => r.MarinaId == marina.Id);
    }

    private sealed record MarinaRollupResult(Guid MarinaId, string MarinaName);
}
