using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MyMarina.Application.SlipAssignments;
using MyMarina.Application.Tenants;
using MyMarina.Domain.Enums;

namespace MyMarina.IntegrationTests;

/// <summary>
/// Tests for slip assignment creation, conflict detection, ending an assignment,
/// and querying with filters.
/// </summary>
public class SlipAssignmentTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _platformClient = factory.CreatePlatformOperatorClient();

    /// <summary>
    /// Creates a full setup: tenant → marina → dock → slip → customer → boat.
    /// Returns the owner client and the IDs needed for slip assignments.
    /// </summary>
    private async Task<(HttpClient Owner, Guid SlipId, Guid CustomerAccountId, Guid BoatId)> SetupAsync()
    {
        var slug = $"sa-{Guid.NewGuid():N}"[..28];
        var tenantResp = await _platformClient.PostAsJsonAsync("/tenants", new
        {
            Name = "Assign Co", Slug = slug,
            OwnerEmail = $"{Guid.NewGuid():N}@ex.com", OwnerFirstName = "A", OwnerLastName = "B",
            OwnerPassword = "OwnerPass@123", SubscriptionTier = SubscriptionTier.Free,
        });
        var tenant = await tenantResp.Content.ReadFromJsonAsync<CreateTenantResult>();
        var owner  = factory.CreateMarinaOwnerClient(tenant!.TenantId);

        // Marina
        var marinaResp = await owner.PostAsJsonAsync("/marinas", new
        {
            Name = "Assign Marina",
            Address = new { Street = "1 St", City = "C", State = "FL", Zip = "33101", Country = "US" },
            PhoneNumber = "555-0000", Email = "a@m.io", TimeZoneId = "America/New_York",
            Website = (string?)null, Description = (string?)null,
        });
        var marinaId = await marinaResp.Content.ReadFromJsonAsync<Guid>();

        // Slip
        var slipResp = await owner.PostAsJsonAsync($"/marinas/{marinaId}/slips", new
        {
            DockId = (Guid?)null, Name = "B-01",
            SlipType = SlipType.Floating, MaxLength = 40m, MaxBeam = 14m, MaxDraft = 5m,
            HasElectric = true, Electric = ElectricService.Amp30, HasWater = true,
            RateType = RateType.Flat, DailyRate = (decimal?)null, MonthlyRate = 650m,
            AnnualRate = (decimal?)null, Status = SlipStatus.Available,
            Latitude = (decimal?)null, Longitude = (decimal?)null, Notes = (string?)null,
        });
        var slipId = await slipResp.Content.ReadFromJsonAsync<Guid>();

        // Customer
        var custResp = await owner.PostAsJsonAsync("/customers", new
        {
            DisplayName = "Anchor Holdings", BillingEmail = $"{Guid.NewGuid():N}@bh.io",
            BillingPhone = (string?)null, BillingAddress = (object?)null,
            EmergencyContactName = (string?)null, EmergencyContactPhone = (string?)null, Notes = (string?)null,
        });
        var custId = await custResp.Content.ReadFromJsonAsync<Guid>();

        // Boat (fits the slip)
        var boatResp = await owner.PostAsJsonAsync($"/customers/{custId}/boats", new
        {
            Name = "Anchor 1", Make = "Catalina", Model = "355", Year = 2019,
            Length = 35m, Beam = 12m, Draft = 4m, BoatType = BoatType.Sailboat,
            HullColor = (string?)null, RegistrationNumber = (string?)null, RegistrationState = (string?)null,
            InsuranceProvider = (string?)null, InsurancePolicyNumber = (string?)null, InsuranceExpiresOn = (DateOnly?)null,
        });
        var boatId = await boatResp.Content.ReadFromJsonAsync<Guid>();

        return (owner, slipId, custId, boatId);
    }

    [Fact]
    public async Task Create_assignment_returns_201_and_appears_in_list()
    {
        var (owner, slipId, custId, boatId) = await SetupAsync();

        var resp = await owner.PostAsJsonAsync("/slip-assignments", new
        {
            SlipId            = slipId,
            CustomerAccountId = custId,
            BoatId            = boatId,
            AssignmentType    = AssignmentType.Annual,
            StartDate         = "2026-05-01",
            EndDate           = "2027-04-30",
            RateOverride      = (decimal?)null,
            Notes             = (string?)null,
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var list = await owner.GetFromJsonAsync<IReadOnlyList<SlipAssignmentDto>>(
            $"/slip-assignments?slipId={slipId}");
        list.Should().ContainSingle(a => a.SlipId == slipId && a.BoatId == boatId);
    }

    [Fact]
    public async Task Overlapping_assignment_returns_409()
    {
        var (owner, slipId, custId, boatId) = await SetupAsync();

        async Task<HttpResponseMessage> AssignAsync(string start, string end) =>
            await owner.PostAsJsonAsync("/slip-assignments", new
            {
                SlipId = slipId, CustomerAccountId = custId, BoatId = boatId,
                AssignmentType = AssignmentType.Monthly,
                StartDate = start, EndDate = end,
                RateOverride = (decimal?)null, Notes = (string?)null,
            });

        // First: June
        var first = await AssignAsync("2026-06-01", "2026-06-30");
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        // Second: overlaps into June
        var second = await AssignAsync("2026-06-15", "2026-07-15");
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Non_overlapping_assignments_are_accepted()
    {
        var (owner, slipId, custId, boatId) = await SetupAsync();

        async Task<HttpResponseMessage> AssignAsync(string start, string end) =>
            await owner.PostAsJsonAsync("/slip-assignments", new
            {
                SlipId = slipId, CustomerAccountId = custId, BoatId = boatId,
                AssignmentType = AssignmentType.Monthly,
                StartDate = start, EndDate = end,
                RateOverride = (decimal?)null, Notes = (string?)null,
            });

        var first  = await AssignAsync("2026-08-01", "2026-08-31");
        var second = await AssignAsync("2026-09-01", "2026-09-30");

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task End_assignment_sets_end_date()
    {
        var (owner, slipId, custId, boatId) = await SetupAsync();

        var createResp = await owner.PostAsJsonAsync("/slip-assignments", new
        {
            SlipId = slipId, CustomerAccountId = custId, BoatId = boatId,
            AssignmentType = AssignmentType.Annual,
            StartDate = "2026-07-01", EndDate = (string?)null,
            RateOverride = (decimal?)null, Notes = (string?)null,
        });
        var assignmentId = await createResp.Content.ReadFromJsonAsync<Guid>();

        var endResp = await owner.PostAsJsonAsync(
            $"/slip-assignments/{assignmentId}/end", new { EndDate = "2026-10-01" });

        endResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await owner.GetFromJsonAsync<IReadOnlyList<SlipAssignmentDto>>(
            $"/slip-assignments?slipId={slipId}");
        var assignment = list!.First(a => a.Id == assignmentId);
        assignment.EndDate.Should().Be(new DateOnly(2026, 10, 1));
    }

    [Fact]
    public async Task ActiveOnly_filter_excludes_past_assignments()
    {
        var (owner, slipId, custId, boatId) = await SetupAsync();

        // Past assignment (ended in the past)
        await owner.PostAsJsonAsync("/slip-assignments", new
        {
            SlipId = slipId, CustomerAccountId = custId, BoatId = boatId,
            AssignmentType = AssignmentType.Monthly,
            StartDate = "2025-01-01", EndDate = "2025-01-31",
            RateOverride = (decimal?)null, Notes = "past",
        });

        // Future assignment
        await owner.PostAsJsonAsync("/slip-assignments", new
        {
            SlipId = slipId, CustomerAccountId = custId, BoatId = boatId,
            AssignmentType = AssignmentType.Monthly,
            StartDate = "2027-06-01", EndDate = "2027-06-30",
            RateOverride = (decimal?)null, Notes = "future",
        });

        var all = await owner.GetFromJsonAsync<IReadOnlyList<SlipAssignmentDto>>(
            $"/slip-assignments?slipId={slipId}&activeOnly=false");
        all.Should().HaveCount(2);

        var active = await owner.GetFromJsonAsync<IReadOnlyList<SlipAssignmentDto>>(
            $"/slip-assignments?slipId={slipId}&activeOnly=true");
        active.Should().ContainSingle(a => a.Notes == "future");
        active.Should().NotContain(a => a.Notes == "past");
    }
}
