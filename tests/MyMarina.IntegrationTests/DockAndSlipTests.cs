using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MyMarina.Application.Docks;
using MyMarina.Application.Slips;
using MyMarina.Application.Tenants;
using MyMarina.Domain.Enums;

namespace MyMarina.IntegrationTests;

/// <summary>
/// Tests for dock and slip CRUD, plus the slip availability query.
/// </summary>
public class DockAndSlipTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _platformClient = factory.CreatePlatformOperatorClient();

    private async Task<(HttpClient Owner, Guid MarinaId)> SetupAsync()
    {
        var slug = $"ds-{Guid.NewGuid():N}"[..28];
        var tenantResp = await _platformClient.PostAsJsonAsync("/tenants", new
        {
            Name = "Dock Slip Co", Slug = slug,
            OwnerEmail = $"{Guid.NewGuid():N}@ex.com", OwnerFirstName = "A", OwnerLastName = "B",
            OwnerPassword = "OwnerPass@123", SubscriptionTier = SubscriptionTier.Free,
        });
        var tenant = await tenantResp.Content.ReadFromJsonAsync<CreateTenantResult>();
        var owner = factory.CreateMarinaOwnerClient(tenant!.TenantId);

        var marinaResp = await owner.PostAsJsonAsync("/marinas", new
        {
            Name = "DS Marina", Address = new { Street = "1 St", City = "City", State = "FL", Zip = "33101", Country = "US" },
            PhoneNumber = "555-0000", Email = "ds@m.io", TimeZoneId = "America/New_York",
            Website = (string?)null, Description = (string?)null,
        });
        var marinaId = await marinaResp.Content.ReadFromJsonAsync<Guid>();

        return (owner, marinaId);
    }

    // ---- Docks ----

    [Fact]
    public async Task Create_dock_returns_201_and_appears_in_list()
    {
        var (owner, marinaId) = await SetupAsync();

        var resp = await owner.PostAsJsonAsync($"/marinas/{marinaId}/docks",
            new { Name = "Dock A", Description = "Main dock", SortOrder = 1 });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var docks = await owner.GetFromJsonAsync<IReadOnlyList<DockDto>>($"/marinas/{marinaId}/docks");
        docks.Should().Contain(d => d.Name == "Dock A");
    }

    [Fact]
    public async Task Update_dock_persists_changes()
    {
        var (owner, marinaId) = await SetupAsync();

        var createResp = await owner.PostAsJsonAsync($"/marinas/{marinaId}/docks",
            new { Name = "Old Dock", Description = (string?)null, SortOrder = 0 });
        var id = await createResp.Content.ReadFromJsonAsync<Guid>();

        var updateResp = await owner.PutAsJsonAsync($"/docks/{id}",
            new { Name = "New Dock", Description = "Updated", SortOrder = 5 });
        updateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var docks = await owner.GetFromJsonAsync<IReadOnlyList<DockDto>>($"/marinas/{marinaId}/docks");
        var dock = docks!.First(d => d.Id == id);
        dock.Name.Should().Be("New Dock");
        dock.SortOrder.Should().Be(5);
    }

    [Fact]
    public async Task Delete_dock_returns_204_and_removes_from_list()
    {
        var (owner, marinaId) = await SetupAsync();

        var createResp = await owner.PostAsJsonAsync($"/marinas/{marinaId}/docks",
            new { Name = "Delete Me", Description = (string?)null, SortOrder = 0 });
        var id = await createResp.Content.ReadFromJsonAsync<Guid>();

        var deleteResp = await owner.DeleteAsync($"/docks/{id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var docks = await owner.GetFromJsonAsync<IReadOnlyList<DockDto>>($"/marinas/{marinaId}/docks");
        docks.Should().NotContain(d => d.Id == id);
    }

    // ---- Slips ----

    [Fact]
    public async Task Create_slip_returns_201_and_appears_in_list()
    {
        var (owner, marinaId) = await SetupAsync();

        var resp = await owner.PostAsJsonAsync($"/marinas/{marinaId}/slips", new
        {
            DockId = (Guid?)null, Name = "A-01",
            SlipType = SlipType.Floating, MaxLength = 40m, MaxBeam = 14m, MaxDraft = 5m,
            HasElectric = true, Electric = ElectricService.Amp30,
            HasWater = true, RateType = RateType.Flat, DailyRate = (decimal?)null,
            MonthlyRate = 600m, AnnualRate = 6000m, Status = SlipStatus.Available,
            Latitude = (decimal?)null, Longitude = (decimal?)null, Notes = (string?)null,
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var slips = await owner.GetFromJsonAsync<IReadOnlyList<SlipDto>>($"/marinas/{marinaId}/slips");
        slips.Should().Contain(s => s.Name == "A-01");
    }

    [Fact]
    public async Task Available_slips_excludes_slips_that_are_too_small()
    {
        var (owner, marinaId) = await SetupAsync();

        // Small slip: max 20ft length
        await owner.PostAsJsonAsync($"/marinas/{marinaId}/slips", new
        {
            DockId = (Guid?)null, Name = "Small-01",
            SlipType = SlipType.Floating, MaxLength = 20m, MaxBeam = 8m, MaxDraft = 3m,
            HasElectric = false, Electric = (ElectricService?)null,
            HasWater = true, RateType = RateType.Flat, DailyRate = 50m,
            MonthlyRate = (decimal?)null, AnnualRate = (decimal?)null, Status = SlipStatus.Available,
            Latitude = (decimal?)null, Longitude = (decimal?)null, Notes = (string?)null,
        });

        // Large slip: max 60ft
        await owner.PostAsJsonAsync($"/marinas/{marinaId}/slips", new
        {
            DockId = (Guid?)null, Name = "Large-01",
            SlipType = SlipType.Floating, MaxLength = 60m, MaxBeam = 18m, MaxDraft = 8m,
            HasElectric = true, Electric = ElectricService.Amp50,
            HasWater = true, RateType = RateType.Flat, DailyRate = (decimal?)null,
            MonthlyRate = 900m, AnnualRate = (decimal?)null, Status = SlipStatus.Available,
            Latitude = (decimal?)null, Longitude = (decimal?)null, Notes = (string?)null,
        });

        // Query for a 45ft boat — only Large-01 should match
        var available = await owner.GetFromJsonAsync<IReadOnlyList<SlipDto>>(
            $"/marinas/{marinaId}/slips/available?boatLength=45&boatBeam=14&boatDraft=5&startDate=2026-06-01");

        available.Should().ContainSingle(s => s.Name == "Large-01");
        available.Should().NotContain(s => s.Name == "Small-01");
    }
}
