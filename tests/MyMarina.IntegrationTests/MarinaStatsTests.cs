using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.IntegrationTests;

[Collection("Integration")]
public class MarinaStatsTests(ApiWebApplicationFactory factory)
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    async Task<(HttpClient client, Guid marinaId)> CreateMarinaWithSlipsAsync(
        int annual = 0, int seasonal = 0, int monthly = 0, int transient = 0,
        int maintenance = 0, int listed = 0, int vacant = 0)
    {
        using var scope = factory.Services.CreateScope();
        var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var userId = Guid.CreateVersion7();
        var email  = $"stats-{userId:N}@example.com";
        var appUser = new ApplicationUser
        {
            Id = userId, UserName = email, Email = email,
            EmailConfirmed = true, FirstName = "Stats", LastName = "Test",
        };
        await userManager.CreateAsync(appUser, "StatsTest!123");

        var tenant = new Tenant { Name = "Stats Tenant", Slug = $"stats-{userId:N}" };
        var marina = new Marina
        {
            TenantId = tenant.Id,
            Name     = "Stats Marina",
            Slug     = $"stats-marina-{userId:N}",
            MarinaType = MarinaType.Commercial,
            IsSetupComplete = true,
            TimeZoneId = "UTC",
        };
        var membership = new Membership
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

        var today = DateOnly.FromDateTime(DateTime.Today);
        var billingAccount = new BillingAccount
        {
            MarinaId     = marina.Id,
            DisplayName  = "Test Account",
            BillingEmail = $"billing-{userId:N}@example.com",
        };
        db.BillingAccounts.Add(billingAccount);
        var vessel = new Vessel { Name = "Test Boat", OwnerUserId = userId, Length = 30, Beam = 10, Draft = 5 };
        db.Vessels.Add(vessel);

        void AddAssignedSlip(AssignmentType type)
        {
            var slip = new Slip { MarinaId = marina.Id, Name = $"Slip-{Guid.NewGuid():N}", MaxLength = 30, MaxBeam = 10, MaxDraft = 5 };
            db.Slips.Add(slip);
            db.SlipAssignments.Add(new SlipAssignment
            {
                SlipId           = slip.Id,
                BillingAccountId = billingAccount.Id,
                VesselId         = vessel.Id,
                AssignmentType   = type,
                StartDate        = today.AddDays(-30),
                BaseRate         = 500m,
            });
        }

        for (var i = 0; i < annual;      i++) AddAssignedSlip(AssignmentType.Annual);
        for (var i = 0; i < seasonal;    i++) AddAssignedSlip(AssignmentType.Seasonal);
        for (var i = 0; i < monthly;     i++) AddAssignedSlip(AssignmentType.Monthly);
        for (var i = 0; i < transient;   i++) AddAssignedSlip(AssignmentType.Transient);

        for (var i = 0; i < maintenance; i++)
        {
            var slip = new Slip { MarinaId = marina.Id, Name = $"Slip-{Guid.NewGuid():N}", MaxLength = 30, MaxBeam = 10, MaxDraft = 5, Status = SlipStatus.UnderMaintenance };
            db.Slips.Add(slip);
        }

        for (var i = 0; i < listed; i++)
        {
            var slip = new Slip { MarinaId = marina.Id, Name = $"Slip-{Guid.NewGuid():N}", MaxLength = 30, MaxBeam = 10, MaxDraft = 5 };
            db.Slips.Add(slip);
            db.AvailabilityWindows.Add(new AvailabilityWindow
            {
                SlipId          = slip.Id,
                ListedByKind    = ListedByKind.Owner,
                ListedByMarinaId = marina.Id,
                ListingKind     = ListingKind.Transient,
                StartsAt        = DateTimeOffset.UtcNow.AddDays(-7),
                EndsAt          = DateTimeOffset.UtcNow.AddDays(60),
                Status          = AvailabilityWindowStatus.Open,
                BasePricePerNight = 75m,
            });
        }

        for (var i = 0; i < vacant; i++)
        {
            var slip = new Slip { MarinaId = marina.Id, Name = $"Slip-{Guid.NewGuid():N}", MaxLength = 30, MaxBeam = 10, MaxDraft = 5 };
            db.Slips.Add(slip);
        }

        await db.SaveChangesAsync();

        var token = TestJwtHelper.UserToken(userId, email,
            memberships: [new MembershipClaim(MembershipScope.Marina, tenant.Id, marina.Id, MembershipRole.Owner, null)]);
        var client = factory.CreateClientWithToken(token);

        return (client, marina.Id);
    }

    // ── GET /marinas/{id}/composition ─────────────────────────────────────────

    [Fact]
    public async Task GetComposition_CorrectlyCounts_AllCategories()
    {
        var (client, marinaId) = await CreateMarinaWithSlipsAsync(
            annual: 2, seasonal: 1, monthly: 3, transient: 1,
            maintenance: 1, listed: 2, vacant: 3);

        var response = await client.GetAsync($"/marinas/{marinaId}/composition");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<MarinaCompositionDto>();
        Assert.NotNull(dto);
        Assert.Equal(13, dto.Total);       // 2+1+3+1+1+2+3
        Assert.Equal(2,  dto.Annual);
        Assert.Equal(1,  dto.Seasonal);
        Assert.Equal(3,  dto.Monthly);
        Assert.Equal(1,  dto.Transient);
        Assert.Equal(1,  dto.Maintenance);
        Assert.Equal(2,  dto.Listed);
        Assert.Equal(3,  dto.Vacant);
    }

    [Fact]
    public async Task GetComposition_EmptyMarina_ReturnsAllZeros()
    {
        var (client, marinaId) = await CreateMarinaWithSlipsAsync();

        var response = await client.GetAsync($"/marinas/{marinaId}/composition");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<MarinaCompositionDto>();
        Assert.NotNull(dto);
        Assert.Equal(0, dto.Total);
        Assert.Equal(0, dto.Vacant);
    }

    [Fact]
    public async Task GetComposition_NoAuth_Returns401()
    {
        var (_, marinaId) = await CreateMarinaWithSlipsAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/marinas/{marinaId}/composition");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetComposition_WrongMarina_Returns403()
    {
        var (client, _) = await CreateMarinaWithSlipsAsync();
        var otherMarinaId = Guid.NewGuid();

        var response = await client.GetAsync($"/marinas/{otherMarinaId}/composition");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── GET /marinas/{id}/billing-summary ─────────────────────────────────────

    [Fact]
    public async Task GetBillingSummary_EmptyMarina_ReturnsZeros()
    {
        var (client, marinaId) = await CreateMarinaWithSlipsAsync();

        var response = await client.GetAsync($"/marinas/{marinaId}/billing-summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<BillingSummaryDto>();
        Assert.NotNull(dto);
        Assert.Equal(0m, dto.TotalOutstanding);
        Assert.Equal(0,  dto.OverdueCount);
        Assert.Equal(0m, dto.CollectedThisMonth);
    }

    [Fact]
    public async Task GetBillingSummary_NoAuth_Returns401()
    {
        var (_, marinaId) = await CreateMarinaWithSlipsAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/marinas/{marinaId}/billing-summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
