using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MyMarina.Application.Search;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.IntegrationTests;

[Collection("Integration")]
public class MarinaRollupSearchTests(ApiWebApplicationFactory factory)
{
    // Chesapeake Bay area coordinates for bounding box
    private const decimal CenterLat = 38.98m;
    private const decimal CenterLon = -76.49m;
    private const decimal Delta      = 0.5m;
    private const decimal North      = CenterLat + Delta;
    private const decimal South      = CenterLat - Delta;
    private const decimal East       = CenterLon + Delta;
    private const decimal West       = CenterLon - Delta;

    private static string BboxQuery(string extra = "")
        => $"marinas/search?north={North}&south={South}&east={East}&west={West}&arrivesAt=2030-07-04&departsAt=2030-07-08{extra}";

    // ── seed helpers ──────────────────────────────────────────────────────────

    private async Task<(Guid tenantId, Guid marinaId)> SeedMarinaAsync(
        string name, decimal lat, decimal lon,
        bool isDemo = false,
        string rateKind = "Flat",
        int slipCount = 3,
        bool hasWindow = true,
        decimal? flatRate = null,
        decimal? perFootRate = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenant = new Domain.Entities.Tenant { Name = name, Slug = $"t-{Guid.NewGuid():N}", IsDemo = isDemo };
        var marina = new Domain.Entities.Marina
        {
            TenantId        = tenant.Id,
            Name            = name,
            Slug            = $"m-{Guid.NewGuid():N}",
            MarinaType      = MarinaType.Commercial,
            IsSetupComplete = true,
            Latitude        = lat,
            Longitude       = lon,
        };
        db.Tenants.Add(tenant);
        db.Marinas.Add(marina);

        var windowStart = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var windowEnd   = new DateTimeOffset(2030, 12, 31, 0, 0, 0, TimeSpan.Zero);

        for (int i = 0; i < slipCount; i++)
        {
            var slip = new Domain.Entities.Slip
            {
                MarinaId  = marina.Id,
                Name      = $"Slip {i + 1}",
                SlipType  = SlipType.Floating,
                Status    = SlipStatus.Active,
                MaxLength = 50,
                MaxBeam   = 16,
                MaxDraft  = 6,
            };

            if (hasWindow)
            {
                var kind = (i % 2 == 0 || rateKind == "Flat") ? RateKind.Flat : RateKind.PerFoot;
                var price = kind == RateKind.Flat
                    ? (flatRate ?? 3200m + i * 100m)
                    : (perFootRate ?? 65m + i * 5m);

                db.AvailabilityWindows.Add(new Domain.Entities.AvailabilityWindow
                {
                    SlipId            = slip.Id,
                    ListingKind       = ListingKind.Transient,
                    RateKind          = kind,
                    BasePricePerNight = price,
                    InstantBook       = true,
                    StartsAt          = windowStart,
                    EndsAt            = windowEnd,
                    Status            = AvailabilityWindowStatus.Open,
                    MinNights         = 1,
                });
            }
            else
            {
                slip.ResolvedTransientBaseRate = flatRate ?? 3000m + i * 100m;
            }

            db.Slips.Add(slip);
        }

        await db.SaveChangesAsync();
        return (tenant.Id, marina.Id);
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Marina_With_Matching_Slips_Returns_Rollup_Row()
    {
        var client = factory.CreateClient();
        await SeedMarinaAsync("Sunset Point", CenterLat, CenterLon, slipCount: 3, flatRate: 3200m);

        var response = await client.GetAsync(BboxQuery());
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<List<MarinaRollupResultDto>>();
        Assert.NotNull(results);

        var row = results.FirstOrDefault(r => r.MarinaName == "Sunset Point");
        Assert.NotNull(row);
        Assert.Equal(3, row.AvailableCount);
        Assert.True(row.InstantBookAvailable);

        // Price fields must not be present in the response
        var json = await client.GetStringAsync(BboxQuery());
        using var doc = JsonDocument.Parse(json);
        var rowEl = doc.RootElement.EnumerateArray()
            .FirstOrDefault(e => e.GetProperty("marinaName").GetString() == "Sunset Point");
        Assert.False(rowEl.TryGetProperty("minPricePerNight", out _), "minPricePerNight must not be in rollup response");
        Assert.False(rowEl.TryGetProperty("maxPricePerNight", out _), "maxPricePerNight must not be in rollup response");
        Assert.False(rowEl.TryGetProperty("rateKind", out _), "rateKind must not be in rollup response");
    }

    [Fact]
    public async Task Marina_With_No_Matching_Slips_Excluded()
    {
        var client = factory.CreateClient();
        await SeedMarinaAsync("TinySlip Marina", CenterLat, CenterLon, slipCount: 2, flatRate: 2500m);

        var response = await client.GetAsync(BboxQuery("&vesselLength=100"));
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<List<MarinaRollupResultDto>>();
        Assert.NotNull(results);
        Assert.DoesNotContain(results, r => r.MarinaName == "TinySlip Marina");
    }

    [Fact]
    public async Task Marina_Outside_Bounding_Box_Excluded()
    {
        var client = factory.CreateClient();
        await SeedMarinaAsync("Florida Marina", 25.0m, -80.0m, slipCount: 2, flatRate: 2500m);

        var response = await client.GetAsync(BboxQuery());
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<List<MarinaRollupResultDto>>();
        Assert.NotNull(results);
        Assert.DoesNotContain(results, r => r.MarinaName == "Florida Marina");
    }

    [Fact]
    public async Task Demo_Marina_Excluded_For_Regular_Session()
    {
        var client = factory.CreateClient();
        await SeedMarinaAsync("Demo Marina", CenterLat, CenterLon, isDemo: true, slipCount: 2, flatRate: 3000m);

        var response = await client.GetAsync(BboxQuery());
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<List<MarinaRollupResultDto>>();
        Assert.NotNull(results);
        Assert.DoesNotContain(results, r => r.MarinaName == "Demo Marina");
    }

    [Fact]
    public async Task Demo_Marina_Included_For_Demo_Session()
    {
        var demoToken = TestJwtHelper.UserToken(Guid.CreateVersion7(), "demo@test.com", isDemo: true);
        var client    = factory.CreateClientWithToken(demoToken);
        await SeedMarinaAsync("Demo Marina Included", CenterLat, CenterLon, isDemo: true, slipCount: 2, flatRate: 3000m);

        var response = await client.GetAsync(BboxQuery());
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<List<MarinaRollupResultDto>>();
        Assert.NotNull(results);
        Assert.Contains(results, r => r.MarinaName == "Demo Marina Included");
    }

    [Fact]
    public async Task Price_Filter_Transient_Includes_Marina_With_InRange_Slip()
    {
        var client = factory.CreateClient();
        // Two slips: one at $150/night (in range), one at $250/night (out of range)
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenant = new Domain.Entities.Tenant { Name = "Price Filter Marina", Slug = $"t-{Guid.NewGuid():N}" };
        var marina = new Domain.Entities.Marina
        {
            TenantId = tenant.Id, Name = "Price Filter Marina", Slug = $"m-{Guid.NewGuid():N}",
            MarinaType = MarinaType.Commercial, IsSetupComplete = true,
            Latitude = CenterLat, Longitude = CenterLon,
        };
        db.Tenants.Add(tenant);
        db.Marinas.Add(marina);

        var windowStart = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var windowEnd   = new DateTimeOffset(2030, 12, 31, 0, 0, 0, TimeSpan.Zero);

        foreach (var price in new[] { 150m, 250m })
        {
            var slip = new Domain.Entities.Slip
            {
                MarinaId = marina.Id, Name = $"Slip {price}", SlipType = SlipType.Floating,
                Status = SlipStatus.Active, MaxLength = 50, MaxBeam = 16, MaxDraft = 6,
            };
            db.Slips.Add(slip);
            db.AvailabilityWindows.Add(new Domain.Entities.AvailabilityWindow
            {
                SlipId = slip.Id, ListingKind = ListingKind.Transient, RateKind = RateKind.Flat,
                BasePricePerNight = price, InstantBook = true,
                StartsAt = windowStart, EndsAt = windowEnd,
                Status = AvailabilityWindowStatus.Open, MinNights = 1,
            });
        }

        await db.SaveChangesAsync();

        var response = await client.GetAsync(BboxQuery("&listingKind=Transient&priceMin=100&priceMax=200"));
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<List<MarinaRollupResultDto>>();
        Assert.NotNull(results);

        var row = results.FirstOrDefault(r => r.MarinaName == "Price Filter Marina");
        Assert.NotNull(row);
        // Only the $150 slip qualifies — count should be 1
        Assert.Equal(1, row.AvailableCount);
    }

    [Fact]
    public async Task Price_Filter_Transient_Excludes_Marina_With_No_InRange_Slip()
    {
        var client = factory.CreateClient();
        // All slips priced above the filter maximum
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenant = new Domain.Entities.Tenant { Name = "Expensive Marina", Slug = $"t-{Guid.NewGuid():N}" };
        var marina = new Domain.Entities.Marina
        {
            TenantId = tenant.Id, Name = "Expensive Marina", Slug = $"m-{Guid.NewGuid():N}",
            MarinaType = MarinaType.Commercial, IsSetupComplete = true,
            Latitude = CenterLat, Longitude = CenterLon,
        };
        db.Tenants.Add(tenant);
        db.Marinas.Add(marina);

        var windowStart = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var windowEnd   = new DateTimeOffset(2030, 12, 31, 0, 0, 0, TimeSpan.Zero);

        for (int i = 0; i < 3; i++)
        {
            var slip = new Domain.Entities.Slip
            {
                MarinaId = marina.Id, Name = $"Slip {i}", SlipType = SlipType.Floating,
                Status = SlipStatus.Active, MaxLength = 50, MaxBeam = 16, MaxDraft = 6,
            };
            db.Slips.Add(slip);
            db.AvailabilityWindows.Add(new Domain.Entities.AvailabilityWindow
            {
                SlipId = slip.Id, ListingKind = ListingKind.Transient, RateKind = RateKind.Flat,
                BasePricePerNight = 500m, InstantBook = true,
                StartsAt = windowStart, EndsAt = windowEnd,
                Status = AvailabilityWindowStatus.Open, MinNights = 1,
            });
        }

        await db.SaveChangesAsync();

        // Price max of $50 — all slips at $500 are out of range
        var response = await client.GetAsync(BboxQuery("&listingKind=Transient&priceMax=50"));
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<List<MarinaRollupResultDto>>();
        Assert.NotNull(results);
        Assert.DoesNotContain(results, r => r.MarinaName == "Expensive Marina");
    }

    [Fact]
    public async Task Price_Filter_Lease_Includes_Marina_With_InRange_Slip()
    {
        var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenant = new Domain.Entities.Tenant { Name = "Lease Price Marina", Slug = $"t-{Guid.NewGuid():N}" };
        var marina = new Domain.Entities.Marina
        {
            TenantId = tenant.Id, Name = "Lease Price Marina", Slug = $"m-{Guid.NewGuid():N}",
            MarinaType = MarinaType.Commercial, IsSetupComplete = true,
            Latitude = CenterLat, Longitude = CenterLon,
        };
        db.Tenants.Add(tenant);
        db.Marinas.Add(marina);

        var windowStart = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var windowEnd   = new DateTimeOffset(2030, 12, 31, 0, 0, 0, TimeSpan.Zero);

        // Slip with $800/mo lease window (in range $500–$1000)
        var slip = new Domain.Entities.Slip
        {
            MarinaId = marina.Id, Name = "Lease Slip", SlipType = SlipType.Floating,
            Status = SlipStatus.Active, MaxLength = 50, MaxBeam = 16, MaxDraft = 6,
        };
        db.Slips.Add(slip);
        db.AvailabilityWindows.Add(new Domain.Entities.AvailabilityWindow
        {
            SlipId = slip.Id, ListingKind = ListingKind.Lease, RateKind = RateKind.Flat,
            LeaseTerm = LeaseTerm.Monthly,
            BasePricePerNight = 800m, InstantBook = false,
            StartsAt = windowStart, EndsAt = windowEnd,
            Status = AvailabilityWindowStatus.Open,
        });

        await db.SaveChangesAsync();

        var response = await client.GetAsync(
            $"marinas/search?north={North}&south={South}&east={East}&west={West}&listingKind=Lease&priceMin=500&priceMax=1000");
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<List<MarinaRollupResultDto>>();
        Assert.NotNull(results);
        Assert.Contains(results, r => r.MarinaName == "Lease Price Marina");
    }

    [Fact]
    public async Task Price_Filter_Lease_Excludes_Marina_With_No_InRange_Slip()
    {
        var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenant = new Domain.Entities.Tenant { Name = "Expensive Lease Marina", Slug = $"t-{Guid.NewGuid():N}" };
        var marina = new Domain.Entities.Marina
        {
            TenantId = tenant.Id, Name = "Expensive Lease Marina", Slug = $"m-{Guid.NewGuid():N}",
            MarinaType = MarinaType.Commercial, IsSetupComplete = true,
            Latitude = CenterLat, Longitude = CenterLon,
        };
        db.Tenants.Add(tenant);
        db.Marinas.Add(marina);

        var windowStart = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var windowEnd   = new DateTimeOffset(2030, 12, 31, 0, 0, 0, TimeSpan.Zero);

        var slip = new Domain.Entities.Slip
        {
            MarinaId = marina.Id, Name = "Expensive Lease Slip", SlipType = SlipType.Floating,
            Status = SlipStatus.Active, MaxLength = 50, MaxBeam = 16, MaxDraft = 6,
        };
        db.Slips.Add(slip);
        db.AvailabilityWindows.Add(new Domain.Entities.AvailabilityWindow
        {
            SlipId = slip.Id, ListingKind = ListingKind.Lease, RateKind = RateKind.Flat,
            LeaseTerm = LeaseTerm.Monthly,
            BasePricePerNight = 3000m, InstantBook = false,
            StartsAt = windowStart, EndsAt = windowEnd,
            Status = AvailabilityWindowStatus.Open,
        });

        await db.SaveChangesAsync();

        var response = await client.GetAsync(
            $"marinas/search?north={North}&south={South}&east={East}&west={West}&listingKind=Lease&priceMax=500");
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<List<MarinaRollupResultDto>>();
        Assert.NotNull(results);
        Assert.DoesNotContain(results, r => r.MarinaName == "Expensive Lease Marina");
    }

    [Fact]
    public async Task Price_Filter_Without_ListingKind_Returns_400()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync(
            $"marinas/search?north={North}&south={South}&east={East}&west={West}&priceMin=100");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Per_Marina_Slip_Search_Returns_Only_That_Marina()
    {
        var client = factory.CreateClient();
        var (_, marina1Id) = await SeedMarinaAsync("Alpha Marina", CenterLat, CenterLon, slipCount: 3, flatRate: 3500m);
        await SeedMarinaAsync("Beta Marina", CenterLat + 0.01m, CenterLon + 0.01m, slipCount: 5, flatRate: 4000m);

        var response = await client.GetAsync(
            $"marinas/{marina1Id}/slips/search?arrivesAt=2030-07-04&departsAt=2030-07-08");
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<List<SlipSearchResultDto>>();
        Assert.NotNull(results);
        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.Equal(marina1Id, r.MarinaId));
    }

    [Fact]
    public async Task Per_Marina_Vessel_Fit_Filter_Applied()
    {
        var client = factory.CreateClient();
        var (_, marinaId) = await SeedMarinaAsync("Fit Filter Marina", CenterLat, CenterLon, slipCount: 3, flatRate: 3200m);

        var response = await client.GetAsync(
            $"marinas/{marinaId}/slips/search?arrivesAt=2030-07-04&departsAt=2030-07-08&vesselLength=100");
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<List<SlipSearchResultDto>>();
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task Per_Marina_Unknown_Id_Returns_404()
    {
        var client   = factory.CreateClient();
        var response = await client.GetAsync(
            $"marinas/{Guid.CreateVersion7()}/slips/search?arrivesAt=2030-07-04&departsAt=2030-07-08");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Per_Marina_Demo_Marina_Returns_404_For_Regular_User()
    {
        var client = factory.CreateClient();
        var (_, demoMarinaId) = await SeedMarinaAsync("Demo Only Marina", CenterLat, CenterLon,
            isDemo: true, slipCount: 2, flatRate: 2500m);

        var response = await client.GetAsync(
            $"marinas/{demoMarinaId}/slips/search?arrivesAt=2030-07-04&departsAt=2030-07-08");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Legacy_Slips_Search_Endpoint_Returns_404()
    {
        var client   = factory.CreateClient();
        var response = await client.GetAsync("slips/search?lat=38.9&lon=-76.4&radiusMiles=25");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
