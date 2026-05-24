using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyMarina.Application.Abstractions;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;
using MyMarina.Infrastructure.Pricing;

namespace MyMarina.IntegrationTests;

[Collection("Integration")]
public class PricingPlanLifecycleTests(ApiWebApplicationFactory factory)
{
    // ── Seed helpers ──────────────────────────────────────────────────────────

    private async Task<(Guid userId, Guid tenantId, Guid marinaId, Guid slipId, HttpClient client)>
        SeedMarinaWithSlipAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userId = Guid.CreateVersion7();
        var email = $"pricing-{userId:N}@example.com";

        var tenant = new Tenant { Name = $"Pricing Tenant {userId:N}", Slug = $"pt-{userId:N}" };
        var marina = new Marina
        {
            TenantId = tenant.Id,
            Name = "Pricing Test Marina",
            Slug = $"pm-{userId:N}",
            MarinaType = MarinaType.Commercial,
            IsSetupComplete = true,
        };
        var slip = new Slip
        {
            MarinaId = marina.Id,
            Name = "T-1",
            SlipType = SlipType.Floating,
            Status = SlipStatus.Active,
            MaxLength = 40m,
            MaxBeam = 14m,
            MaxDraft = 6m,
        };
        var membership = new Membership
        {
            UserId = userId,
            Scope = MembershipScope.Marina,
            TenantId = tenant.Id,
            MarinaId = marina.Id,
            Role = MembershipRole.Owner,
            AcceptedAt = DateTimeOffset.UtcNow,
        };

        db.Tenants.Add(tenant);
        db.Marinas.Add(marina);
        db.Slips.Add(slip);
        db.Memberships.Add(membership);
        await db.SaveChangesAsync();

        var token = TestJwtHelper.UserToken(userId, email,
            memberships: [new MembershipClaim(MembershipScope.Marina, tenant.Id, marina.Id, MembershipRole.Owner, null)]);
        return (userId, tenant.Id, marina.Id, slip.Id, factory.CreateClientWithToken(token));
    }

    private async Task RunRecomputeJobAsync(Guid slipId)
    {
        using var scope = factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<RecomputeSlipPriceJob>();
        await job.ExecuteAsync(slipId);
    }

    // ── Task 16.1: CRUD lifecycle ─────────────────────────────────────────────

    [Fact]
    public async Task CreatePlan_ViaApi_Returns201()
    {
        var (_, _, marinaId, _, client) = await SeedMarinaWithSlipAsync();

        var resp = await client.PostAsJsonAsync(
            $"marinas/{marinaId}/pricing/plans",
            new
            {
                name = "Standard",
                isDefault = true,
                transientRateKind = "Flat",
                transientAmount = 75.0,
                transientMinCharge = (decimal?)null,
                leaseRateKind = (string?)null,
                leaseAmount = (decimal?)null,
                leaseMinCharge = (decimal?)null,
                amenityAddOns = Array.Empty<object>(),
            });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var json = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Standard", json.GetProperty("name").GetString());
        Assert.True(json.GetProperty("isDefault").GetBoolean());
    }

    [Fact]
    public async Task GetPlan_Returns200WithCorrectData()
    {
        var (_, _, marinaId, _, client) = await SeedMarinaWithSlipAsync();

        var createResp = await client.PostAsJsonAsync(
            $"marinas/{marinaId}/pricing/plans",
            new
            {
                name = "PerFoot Plan",
                isDefault = true,
                transientRateKind = "PerFoot",
                transientAmount = 5.0,
                transientMinCharge = 50.0,
                leaseRateKind = (string?)null,
                leaseAmount = (decimal?)null,
                leaseMinCharge = (decimal?)null,
                amenityAddOns = Array.Empty<object>(),
            });
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var planId = created.GetProperty("id").GetGuid();

        var getResp = await client.GetAsync($"marinas/{marinaId}/pricing/plans/{planId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var plan = await getResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("PerFoot", plan.GetProperty("transientRateKind").GetString());
        Assert.Equal(5.0m, plan.GetProperty("transientAmount").GetDecimal());
        Assert.Equal(50.0m, plan.GetProperty("transientMinCharge").GetDecimal());
    }

    [Fact]
    public async Task DeletePlan_Returns204()
    {
        var (_, _, marinaId, _, client) = await SeedMarinaWithSlipAsync();

        var createResp = await client.PostAsJsonAsync(
            $"marinas/{marinaId}/pricing/plans",
            new
            {
                name = "Temp Plan",
                isDefault = false,
                transientRateKind = "Flat",
                transientAmount = 50.0,
                transientMinCharge = (decimal?)null,
                leaseRateKind = (string?)null,
                leaseAmount = (decimal?)null,
                leaseMinCharge = (decimal?)null,
                amenityAddOns = Array.Empty<object>(),
            });
        var created = await createResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var planId = created.GetProperty("id").GetGuid();

        var deleteResp = await client.DeleteAsync($"marinas/{marinaId}/pricing/plans/{planId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
    }

    // ── Task 16.2: Set default ─────────────────────────────────────────────────

    [Fact]
    public async Task SetDefault_SwapsDefaultFlag()
    {
        var (_, _, marinaId, _, client) = await SeedMarinaWithSlipAsync();

        // Create plan A as default
        var aResp = await client.PostAsJsonAsync($"marinas/{marinaId}/pricing/plans",
            new { name = "Plan A", isDefault = true, transientRateKind = "Flat", transientAmount = 50.0,
                  transientMinCharge = (decimal?)null, leaseRateKind = (string?)null, leaseAmount = (decimal?)null,
                  leaseMinCharge = (decimal?)null, amenityAddOns = Array.Empty<object>() });
        var a = await aResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var planAId = a.GetProperty("id").GetGuid();

        // Create plan B as non-default
        var bResp = await client.PostAsJsonAsync($"marinas/{marinaId}/pricing/plans",
            new { name = "Plan B", isDefault = false, transientRateKind = "Flat", transientAmount = 80.0,
                  transientMinCharge = (decimal?)null, leaseRateKind = (string?)null, leaseAmount = (decimal?)null,
                  leaseMinCharge = (decimal?)null, amenityAddOns = Array.Empty<object>() });
        var b = await bResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var planBId = b.GetProperty("id").GetGuid();

        // Set B as default
        var setDefaultResp = await client.PostAsync($"marinas/{marinaId}/pricing/plans/{planBId}/set-default", null);
        Assert.Equal(HttpStatusCode.OK, setDefaultResp.StatusCode);

        // Verify B is default and A is not
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var planA = await db.PricingPlans.IgnoreQueryFilters().FirstAsync(p => p.Id == planAId);
        var planB = await db.PricingPlans.IgnoreQueryFilters().FirstAsync(p => p.Id == planBId);
        Assert.False(planA.IsDefault);
        Assert.True(planB.IsDefault);
    }

    // ── Task 16.3: Bulk assign ────────────────────────────────────────────────

    [Fact]
    public async Task BulkAssign_AssignsSlipsToNewPlan()
    {
        var (_, _, marinaId, slipId, client) = await SeedMarinaWithSlipAsync();

        var planResp = await client.PostAsJsonAsync($"marinas/{marinaId}/pricing/plans",
            new { name = "Bulk Target", isDefault = false, transientRateKind = "Flat", transientAmount = 60.0,
                  transientMinCharge = (decimal?)null, leaseRateKind = (string?)null, leaseAmount = (decimal?)null,
                  leaseMinCharge = (decimal?)null, amenityAddOns = Array.Empty<object>() });
        var plan = await planResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var planId = plan.GetProperty("id").GetGuid();

        var bulkResp = await client.PostAsJsonAsync(
            $"marinas/{marinaId}/pricing/plans/bulk-assign",
            new { pricingPlanId = planId });

        Assert.Equal(HttpStatusCode.OK, bulkResp.StatusCode);
        var result = await bulkResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(result.GetProperty("assignedCount").GetInt32() > 0);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var slip = await db.Slips.FindAsync(slipId);
        Assert.Equal(planId, slip!.PricingPlanId);
    }

    // ── Task 16.4: Resolved price recomputed after plan assigned ──────────────

    [Fact]
    public async Task AssignPlan_ThenRunJob_UpdatesResolvedPrice()
    {
        var (_, _, marinaId, slipId, client) = await SeedMarinaWithSlipAsync();

        // Create a flat plan at $90/night
        var planResp = await client.PostAsJsonAsync($"marinas/{marinaId}/pricing/plans",
            new { name = "Flat 90", isDefault = true, transientRateKind = "Flat", transientAmount = 90.0,
                  transientMinCharge = (decimal?)null, leaseRateKind = (string?)null, leaseAmount = (decimal?)null,
                  leaseMinCharge = (decimal?)null, amenityAddOns = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Created, planResp.StatusCode);

        // Run the recompute job for this slip
        await RunRecomputeJobAsync(slipId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var slip = await db.Slips.FindAsync(slipId);

        Assert.Equal(90m, slip!.ResolvedTransientBaseRate);
        Assert.Null(slip.ResolvedLeaseBaseRate); // plan has no lease rate
    }

    [Fact]
    public async Task AssignPlan_PerFootRate_ComputesCorrectResolved()
    {
        // Slip is 40ft, plan is $5/ft → expected resolved = $200
        var (_, _, marinaId, slipId, client) = await SeedMarinaWithSlipAsync();

        var planResp = await client.PostAsJsonAsync($"marinas/{marinaId}/pricing/plans",
            new { name = "PerFoot 5", isDefault = true, transientRateKind = "PerFoot", transientAmount = 5.0,
                  transientMinCharge = (decimal?)null, leaseRateKind = (string?)null, leaseAmount = (decimal?)null,
                  leaseMinCharge = (decimal?)null, amenityAddOns = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Created, planResp.StatusCode);

        await RunRecomputeJobAsync(slipId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var slip = await db.Slips.FindAsync(slipId);

        Assert.Equal(200m, slip!.ResolvedTransientBaseRate); // 5 * 40
    }

    [Fact]
    public async Task AssignPlan_WithMinCharge_AppliesFloor()
    {
        // Slip is 40ft, plan is $1/ft = $40 raw; minCharge = $75 → resolved $75
        var (_, _, marinaId, slipId, client) = await SeedMarinaWithSlipAsync();

        var planResp = await client.PostAsJsonAsync($"marinas/{marinaId}/pricing/plans",
            new { name = "MinCharge", isDefault = true, transientRateKind = "PerFoot", transientAmount = 1.0,
                  transientMinCharge = 75.0, leaseRateKind = (string?)null, leaseAmount = (decimal?)null,
                  leaseMinCharge = (decimal?)null, amenityAddOns = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Created, planResp.StatusCode);

        await RunRecomputeJobAsync(slipId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var slip = await db.Slips.FindAsync(slipId);

        Assert.Equal(75m, slip!.ResolvedTransientBaseRate);
    }

    [Fact]
    public async Task NoDefaultPlan_SlipResolvedRateIsNull()
    {
        var (_, _, _, slipId, _) = await SeedMarinaWithSlipAsync();

        // Run job without any plan in the marina
        await RunRecomputeJobAsync(slipId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var slip = await db.Slips.FindAsync(slipId);

        Assert.Null(slip!.ResolvedTransientBaseRate);
        Assert.Null(slip.ResolvedLeaseBaseRate);
    }

    // ── Task 16.5: No default plan → 422 when setting IsListed ───────────────

    [Fact]
    public async Task UpdateMarina_IsListed_NoDefaultPlan_Returns422()
    {
        var (_, _, marinaId, _, client) = await SeedMarinaWithSlipAsync();

        var resp = await client.PatchAsJsonAsync(
            $"marinas/{marinaId}",
            new { isListed = true });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateMarina_IsListed_WithDefaultPlan_Returns200()
    {
        var (_, _, marinaId, _, client) = await SeedMarinaWithSlipAsync();

        // Create a default plan first
        await client.PostAsJsonAsync($"marinas/{marinaId}/pricing/plans",
            new { name = "Default", isDefault = true, transientRateKind = "Flat", transientAmount = 50.0,
                  transientMinCharge = (decimal?)null, leaseRateKind = (string?)null, leaseAmount = (decimal?)null,
                  leaseMinCharge = (decimal?)null, amenityAddOns = Array.Empty<object>() });

        var resp = await client.PatchAsJsonAsync(
            $"marinas/{marinaId}",
            new { isListed = true });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── Task 16.6: SlipAssignment renewal uses resolved rate ──────────────────

    [Fact]
    public async Task RenewAssignment_UsesResolvedLeaseRate()
    {
        var (userId, tenantId, marinaId, slipId, client) = await SeedMarinaWithSlipAsync();

        // Create a lease plan and resolve the slip
        var planResp = await client.PostAsJsonAsync($"marinas/{marinaId}/pricing/plans",
            new { name = "Lease Plan", isDefault = true, transientRateKind = (string?)null, transientAmount = (decimal?)null,
                  transientMinCharge = (decimal?)null, leaseRateKind = "Flat", leaseAmount = 3000.0,
                  leaseMinCharge = (decimal?)null, amenityAddOns = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Created, planResp.StatusCode);

        await RunRecomputeJobAsync(slipId);

        Guid billingAccountId, vesselId, assignmentId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var vessel = new Vessel
            {
                OwnerUserId = userId,
                Name = "Test Boat",
                Make = "Test",
                Model = "30",
                Year = 2020,
                Length = 35m,
                Beam = 12m,
                Draft = 4m,
            };
            db.Vessels.Add(vessel);

            var ba = new BillingAccount
            {
                MarinaId = marinaId,
                DisplayName = "Test Account",
                BillingEmail = $"billing-{userId:N}@example.com",
            };
            db.BillingAccounts.Add(ba);
            db.BillingAccountMembers.Add(new BillingAccountMember
            {
                BillingAccountId = ba.Id,
                UserId = userId,
                Role = BillingAccountRole.Owner,
            });

            // Create assignment at the resolved rate
            var assignment = new SlipAssignment
            {
                SlipId = slipId,
                BillingAccountId = ba.Id,
                VesselId = vessel.Id,
                AssignmentType = AssignmentType.Annual,
                StartDate = new DateOnly(2025, 1, 1),
                EndDate = new DateOnly(2025, 12, 31),
                BaseRate = 3000m,
            };
            db.SlipAssignments.Add(assignment);
            await db.SaveChangesAsync();

            billingAccountId = ba.Id;
            vesselId = vessel.Id;
            assignmentId = assignment.Id;
        }

        var renewResp = await client.PostAsJsonAsync(
            $"marinas/{marinaId}/slip-assignments/{assignmentId}/renew",
            new { newStartDate = "2026-01-01", newEndDate = "2026-12-31" });

        Assert.Equal(HttpStatusCode.Created, renewResp.StatusCode);
        var renewal = await renewResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(3000m, renewal.GetProperty("baseRate").GetDecimal());
    }

    [Fact]
    public async Task RenewAssignment_NoResolvedLeaseRate_Returns409()
    {
        var (userId, tenantId, marinaId, slipId, client) = await SeedMarinaWithSlipAsync();

        // No lease plan — resolved lease rate is null
        // Create assignment manually
        Guid assignmentId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var vessel = new Vessel
            {
                OwnerUserId = userId,
                Name = "No Lease Boat",
                Make = "Test",
                Model = "30",
                Year = 2020,
                Length = 35m,
                Beam = 12m,
                Draft = 4m,
            };
            db.Vessels.Add(vessel);

            var ba = new BillingAccount
            {
                MarinaId = marinaId,
                DisplayName = "No Lease Acct",
                BillingEmail = $"nolease-{userId:N}@example.com",
            };
            db.BillingAccounts.Add(ba);
            db.BillingAccountMembers.Add(new BillingAccountMember
            {
                BillingAccountId = ba.Id,
                UserId = userId,
                Role = BillingAccountRole.Owner,
            });

            var assignment = new SlipAssignment
            {
                SlipId = slipId,
                BillingAccountId = ba.Id,
                VesselId = vessel.Id,
                AssignmentType = AssignmentType.Annual,
                StartDate = new DateOnly(2025, 1, 1),
                EndDate = new DateOnly(2025, 12, 31),
                BaseRate = 3000m,
            };
            db.SlipAssignments.Add(assignment);
            await db.SaveChangesAsync();
            assignmentId = assignment.Id;
        }

        var renewResp = await client.PostAsJsonAsync(
            $"marinas/{marinaId}/slip-assignments/{assignmentId}/renew",
            new { newStartDate = "2026-01-01", newEndDate = "2026-12-31" });

        Assert.Equal(HttpStatusCode.Conflict, renewResp.StatusCode);
    }
}
