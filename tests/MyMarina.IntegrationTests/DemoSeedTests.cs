using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyMarina.Infrastructure.Demo;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.IntegrationTests;

[Collection("Integration")]
public class DemoSeedTests(ApiWebApplicationFactory factory)
{
    async Task EnsureSeeded()
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DemoSeedScript>().SeedAsync();
    }

    [Fact]
    public async Task DemoSeed_PopulatesAllEntityTypes()
    {
        await EnsureSeeded();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(await db.Tenants.AnyAsync(t => t.IsDemo),                                                "Tenant");
        Assert.True(await db.Marinas.AnyAsync(m => m.TenantId == DemoSeedScript.TenantId),                   "Marina");
        Assert.True(await db.Docks.AnyAsync(d => d.MarinaId == DemoSeedScript.SunsetBayId),                  "Dock");
        Assert.True(await db.Slips.AnyAsync(s => s.MarinaId == DemoSeedScript.SunsetBayId),                  "Slip");
        Assert.True(await db.Memberships.AnyAsync(m => m.TenantId == DemoSeedScript.TenantId),               "Membership");
        Assert.True(await db.BillingAccounts.AnyAsync(b => b.MarinaId == DemoSeedScript.SunsetBayId),        "BillingAccount");
        Assert.True(await db.BillingAccountMembers.AnyAsync(b => b.UserId == DemoSeedScript.DemoUserId),     "BillingAccountMember");
        Assert.True(await db.Vessels.AnyAsync(v => v.OwnerUserId == DemoSeedScript.DemoUserId),              "Vessel");
        Assert.True(await db.MarinaVesselRecords.AnyAsync(r => r.MarinaId == DemoSeedScript.SunsetBayId),    "MarinaVesselRecord");
        Assert.True(await db.SlipAssignments.AnyAsync(),                                                      "SlipAssignment");
        Assert.True(await db.AvailabilityWindows.AnyAsync(w => w.ListedByMarinaId == DemoSeedScript.SunsetBayId), "AvailabilityWindow");
        Assert.True(await db.Reservations.AnyAsync(r => r.BoaterUserId == DemoSeedScript.DemoUserId),        "Reservation");
        Assert.True(await db.Invoices.AnyAsync(i => i.MarinaId == DemoSeedScript.SunsetBayId),               "Invoice");
        Assert.True(await db.InvoiceLineItems.AnyAsync(),                                                     "InvoiceLineItem");
        Assert.True(await db.Payments.AnyAsync(),                                                             "Payment");
        Assert.True(await db.MaintenanceRequests.AnyAsync(r => r.MarinaId == DemoSeedScript.SunsetBayId),    "MaintenanceRequest");
        Assert.True(await db.WorkOrders.AnyAsync(w => w.MarinaId == DemoSeedScript.SunsetBayId),             "WorkOrder");
        Assert.True(await db.Announcements.AnyAsync(a => a.MarinaId == DemoSeedScript.SunsetBayId),          "Announcement");
        Assert.True(await db.MarinaPhotos.AnyAsync(p => p.MarinaId == DemoSeedScript.SunsetBayId),            "MarinaPhoto");
        Assert.True(await db.PricingPlans.AnyAsync(p => p.MarinaId == DemoSeedScript.SunsetBayId),            "PricingPlan");
    }

    [Fact]
    public async Task DemoSeed_IsIdempotent()
    {
        // Run twice — second call must not throw or create duplicate tenants.
        await EnsureSeeded();
        await EnsureSeeded();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.Tenants.CountAsync(t => t.Id == DemoSeedScript.TenantId));
    }

    [Fact]
    public async Task DemoSession_Endpoint_Returns200WithToken()
    {
        await EnsureSeeded();

        var client   = factory.CreateClient();
        var response = await client.PostAsync("/demo/session", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<DemoSessionBody>();
        Assert.NotNull(body?.AccessToken);
        Assert.True(body!.IsDemo);
    }

    [Fact]
    public async Task DemoSession_WriteRequest_Returns403()
    {
        await EnsureSeeded();

        // Get a demo token.
        var anonClient  = factory.CreateClient();
        var sessionResp = await anonClient.PostAsync("/demo/session", null);
        var session     = await sessionResp.Content.ReadFromJsonAsync<DemoSessionBody>();
        Assert.NotNull(session?.AccessToken);

        // Attempt a write with the demo token.
        var demoClient = factory.CreateClientWithToken(session!.AccessToken);
        var response   = await demoClient.PostAsJsonAsync("/vessels", new
        {
            name = "Hacked Vessel", length = 30, beam = 10, draft = 3, boatType = "Powerboat"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DemoSession_PricingRuleWrite_Returns403()
    {
        await EnsureSeeded();

        var anonClient  = factory.CreateClient();
        var sessionResp = await anonClient.PostAsync("/demo/session", null);
        var session     = await sessionResp.Content.ReadFromJsonAsync<DemoSessionBody>();
        Assert.NotNull(session?.AccessToken);

        var demoClient = factory.CreateClientWithToken(session!.AccessToken);
        var response   = await demoClient.PostAsJsonAsync(
            $"/marinas/{DemoSeedScript.SunsetBayId}/pricing/rules",
            new { name = "Hacked rule", contributionKind = "Base" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record DemoSessionBody(string AccessToken, string ExpiresAt, bool IsDemo);
}
