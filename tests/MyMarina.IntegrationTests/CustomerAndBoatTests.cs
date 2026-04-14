using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MyMarina.Application.Boats;
using MyMarina.Application.Customers;
using MyMarina.Application.Tenants;
using MyMarina.Domain.Enums;

namespace MyMarina.IntegrationTests;

/// <summary>
/// Tests for customer account CRUD (including deactivation) and boat CRUD.
/// </summary>
public class CustomerAndBoatTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _platformClient = factory.CreatePlatformOperatorClient();

    private async Task<HttpClient> SetupOwnerAsync()
    {
        var slug = $"cb-{Guid.NewGuid():N}"[..28];
        var resp = await _platformClient.PostAsJsonAsync("/tenants", new
        {
            Name = "Cust Co", Slug = slug,
            OwnerEmail = $"{Guid.NewGuid():N}@ex.com", OwnerFirstName = "X", OwnerLastName = "Y",
            OwnerPassword = "OwnerPass@123", SubscriptionTier = SubscriptionTier.Free,
        });
        var tenant = await resp.Content.ReadFromJsonAsync<CreateTenantResult>();
        return factory.CreateMarinaOwnerClient(tenant!.TenantId);
    }

    private static object CustomerPayload(string displayName = "Smith Family") => new
    {
        DisplayName           = displayName,
        BillingEmail          = $"{Guid.NewGuid():N}@billing.io",
        BillingPhone          = "555-1234",
        BillingAddress        = new { Street = "1 Main St", City = "Town", State = "FL", Zip = "33101", Country = "US" },
        EmergencyContactName  = "Jane Smith",
        EmergencyContactPhone = "555-9999",
        Notes                 = (string?)null,
    };

    // ---- Customers ----

    [Fact]
    public async Task Create_customer_returns_201_and_appears_in_list()
    {
        var owner = await SetupOwnerAsync();

        var resp = await owner.PostAsJsonAsync("/customers", CustomerPayload());

        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var list = await owner.GetFromJsonAsync<IReadOnlyList<CustomerAccountDto>>("/customers");
        list.Should().ContainSingle(c => c.DisplayName == "Smith Family");
    }

    [Fact]
    public async Task Get_customer_by_id_returns_full_record()
    {
        var owner = await SetupOwnerAsync();

        var createResp = await owner.PostAsJsonAsync("/customers", CustomerPayload("Jones Boating"));
        var id = await createResp.Content.ReadFromJsonAsync<Guid>();

        var customer = await owner.GetFromJsonAsync<CustomerAccountDto>($"/customers/{id}");
        customer.Should().NotBeNull();
        customer!.DisplayName.Should().Be("Jones Boating");
        customer.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Update_customer_persists_changes()
    {
        var owner = await SetupOwnerAsync();

        var createResp = await owner.PostAsJsonAsync("/customers", CustomerPayload("Before"));
        var id = await createResp.Content.ReadFromJsonAsync<Guid>();

        var updateResp = await owner.PutAsJsonAsync($"/customers/{id}", new
        {
            DisplayName           = "After",
            BillingEmail          = "after@billing.io",
            BillingPhone          = (string?)null,
            BillingAddress        = (object?)null,
            EmergencyContactName  = (string?)null,
            EmergencyContactPhone = (string?)null,
            Notes                 = "Updated note",
        });

        updateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var customer = await owner.GetFromJsonAsync<CustomerAccountDto>($"/customers/{id}");
        customer!.DisplayName.Should().Be("After");
        customer.Notes.Should().Be("Updated note");
    }

    [Fact]
    public async Task Deactivate_customer_sets_IsActive_false()
    {
        var owner = await SetupOwnerAsync();

        var createResp = await owner.PostAsJsonAsync("/customers", CustomerPayload("Deactivate Me"));
        var id = await createResp.Content.ReadFromJsonAsync<Guid>();

        var deactivateResp = await owner.PostAsync($"/customers/{id}/deactivate", null);
        deactivateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var customer = await owner.GetFromJsonAsync<CustomerAccountDto>($"/customers/{id}");
        customer!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Get_unknown_customer_returns_404()
    {
        var owner = await SetupOwnerAsync();
        var resp = await owner.GetAsync($"/customers/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- Boats ----

    [Fact]
    public async Task Create_boat_returns_201_and_appears_in_customer_list()
    {
        var owner = await SetupOwnerAsync();

        var custResp = await owner.PostAsJsonAsync("/customers", CustomerPayload());
        var custId   = await custResp.Content.ReadFromJsonAsync<Guid>();

        var boatResp = await owner.PostAsJsonAsync($"/customers/{custId}/boats", new
        {
            Name               = "Sea Breeze",
            Make               = "Bayliner",
            Model              = "325",
            Year               = 2018,
            Length             = 32m,
            Beam               = 10m,
            Draft              = 3m,
            BoatType           = BoatType.Powerboat,
            HullColor          = "White",
            RegistrationNumber = "FL-1234-AB",
            RegistrationState  = "FL",
            InsuranceProvider  = (string?)null,
            InsurancePolicyNumber = (string?)null,
            InsuranceExpiresOn = (DateOnly?)null,
        });

        boatResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var boats = await owner.GetFromJsonAsync<IReadOnlyList<BoatDto>>($"/customers/{custId}/boats");
        boats.Should().ContainSingle(b => b.Name == "Sea Breeze");
    }

    [Fact]
    public async Task Update_boat_persists_changes()
    {
        var owner = await SetupOwnerAsync();

        var custResp = await owner.PostAsJsonAsync("/customers", CustomerPayload());
        var custId   = await custResp.Content.ReadFromJsonAsync<Guid>();

        var boatResp = await owner.PostAsJsonAsync($"/customers/{custId}/boats", new
        {
            Name = "Old Boat", Make = (string?)null, Model = (string?)null, Year = (int?)null,
            Length = 25m, Beam = 8m, Draft = 2m, BoatType = BoatType.Sailboat,
            HullColor = (string?)null, RegistrationNumber = (string?)null, RegistrationState = (string?)null,
            InsuranceProvider = (string?)null, InsurancePolicyNumber = (string?)null, InsuranceExpiresOn = (DateOnly?)null,
        });
        var boatId = await boatResp.Content.ReadFromJsonAsync<Guid>();

        var updateResp = await owner.PutAsJsonAsync($"/boats/{boatId}", new
        {
            Name = "New Boat", Make = "Hunter", Model = "27", Year = 2020,
            Length = 27m, Beam = 9m, Draft = 2.5m, BoatType = BoatType.Sailboat,
            HullColor = "Blue", RegistrationNumber = "FL-0000-ZZ", RegistrationState = "FL",
            InsuranceProvider = (string?)null, InsurancePolicyNumber = (string?)null, InsuranceExpiresOn = (DateOnly?)null,
        });

        updateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var boats = await owner.GetFromJsonAsync<IReadOnlyList<BoatDto>>($"/customers/{custId}/boats");
        var boat = boats!.First(b => b.Id == boatId);
        boat.Name.Should().Be("New Boat");
        boat.Make.Should().Be("Hunter");
    }
}
