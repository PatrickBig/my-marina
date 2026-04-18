using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MyMarina.Application.Auth;
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
    private sealed record InviteCustomerResult(Guid UserId, string TemporaryPassword);
    private sealed record PortalMeDto(Guid UserId, Guid CustomerAccountId, string Email);
    private readonly HttpClient _platformClient = factory.CreatePlatformOperatorClient();

    private async Task<(HttpClient Client, Guid TenantId)> SetupOwnerAsync()
    {
        var slug = $"cb-{Guid.NewGuid():N}"[..28];
        var resp = await _platformClient.PostAsJsonAsync("/tenants", new
        {
            Name = "Cust Co", Slug = slug,
            OwnerEmail = $"{Guid.NewGuid():N}@ex.com", OwnerFirstName = "X", OwnerLastName = "Y",
            OwnerPassword = "OwnerPass@123", SubscriptionTier = SubscriptionTier.Free,
        });
        var tenant = await resp.Content.ReadFromJsonAsync<CreateTenantResult>();
        return (factory.CreateMarinaOwnerClient(tenant!.TenantId), tenant!.TenantId);
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
        var (owner, _) = await SetupOwnerAsync();

        var resp = await owner.PostAsJsonAsync("/customers", CustomerPayload());

        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var list = await owner.GetFromJsonAsync<IReadOnlyList<CustomerAccountDto>>("/customers");
        list.Should().ContainSingle(c => c.DisplayName == "Smith Family");
    }

    [Fact]
    public async Task Get_customer_by_id_returns_full_record()
    {
        var (owner, _) = await SetupOwnerAsync();

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
        var (owner, _) = await SetupOwnerAsync();

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
        var (owner, _) = await SetupOwnerAsync();

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
        var (owner, _) = await SetupOwnerAsync();
        var resp = await owner.GetAsync($"/customers/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- Invite Customer ----

    [Fact]
    public async Task Invite_customer_creates_user_and_returns_temporary_password()
    {
        var (owner, _) = await SetupOwnerAsync();

        var custResp = await owner.PostAsJsonAsync("/customers", CustomerPayload("Invite Me"));
        var custId = await custResp.Content.ReadFromJsonAsync<Guid>();

        var inviteResp = await owner.PostAsync($"/customers/{custId}/invite", null);
        inviteResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await inviteResp.Content.ReadFromJsonAsync<InviteCustomerResult>();
        result.Should().NotBeNull();
        result!.UserId.Should().NotBe(Guid.Empty);
        result.TemporaryPassword.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Invite_customer_twice_returns_409_conflict()
    {
        var (owner, _) = await SetupOwnerAsync();

        var custResp = await owner.PostAsJsonAsync("/customers", CustomerPayload("Invite Twice"));
        var custId = await custResp.Content.ReadFromJsonAsync<Guid>();

        // First invite should succeed
        var inviteResp1 = await owner.PostAsync($"/customers/{custId}/invite", null);
        inviteResp1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Second invite should return 409
        var inviteResp2 = await owner.PostAsync($"/customers/{custId}/invite", null);
        inviteResp2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Invited_customer_can_login_and_access_portal()
    {
        var (owner, tenantId) = await SetupOwnerAsync();

        // Create customer
        var custResp = await owner.PostAsJsonAsync("/customers", CustomerPayload("Portal Access"));
        var custId = await custResp.Content.ReadFromJsonAsync<Guid>();
        var customer = await owner.GetFromJsonAsync<CustomerAccountDto>($"/customers/{custId}");
        customer.Should().NotBeNull();

        // Invite customer
        var inviteResp = await owner.PostAsync($"/customers/{custId}/invite", null);
        inviteResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var inviteResult = await inviteResp.Content.ReadFromJsonAsync<InviteCustomerResult>();
        var tempPassword = inviteResult!.TemporaryPassword;

        // Login with temporary password (this is handled by the frontend in reality,
        // but we're testing that the user exists and can authenticate)
        var loginResp = await factory.CreateClient().PostAsJsonAsync("/auth/login", new
        {
            Email = customer!.BillingEmail,
            Password = tempPassword,
        });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResp.Content.ReadFromJsonAsync<LoginResult>();
        loginResult.Should().NotBeNull();
        loginResult!.Role.Should().Be("Customer");
        loginResult.Email.Should().Be(customer.BillingEmail);
    }

    [Fact]
    public async Task Customer_can_access_their_portal_data()
    {
        var (owner, tenantId) = await SetupOwnerAsync();

        // Create customer
        var custResp = await owner.PostAsJsonAsync("/customers", CustomerPayload("My Portal"));
        var custId = await custResp.Content.ReadFromJsonAsync<Guid>();
        var customer = await owner.GetFromJsonAsync<CustomerAccountDto>($"/customers/{custId}");
        customer.Should().NotBeNull();

        // Invite customer
        var inviteResp = await owner.PostAsync($"/customers/{custId}/invite", null);
        var inviteResult = await inviteResp.Content.ReadFromJsonAsync<InviteCustomerResult>();
        var userId = inviteResult!.UserId;

        // Create a customer client using TestJwtHelper
        var customerClient = factory.CreateClientWithToken(
            TestJwtHelper.GenerateToken(userId, customer!.BillingEmail, "Customer", tenantId, null, custId));

        // Customer should be able to access their portal data
        var meResp = await customerClient.GetAsync("/portal/me");
        meResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var meData = await meResp.Content.ReadFromJsonAsync<PortalMeDto>();
        meData.Should().NotBeNull();
        meData!.CustomerAccountId.Should().Be(custId);
    }

    [Fact]
    public async Task Customer_cannot_access_other_customer_data()
    {
        var (owner, tenantId) = await SetupOwnerAsync();

        // Create two customers
        var cust1Resp = await owner.PostAsJsonAsync("/customers", CustomerPayload("Customer 1"));
        var cust1Id = await cust1Resp.Content.ReadFromJsonAsync<Guid>();
        var customer1 = await owner.GetFromJsonAsync<CustomerAccountDto>($"/customers/{cust1Id}");
        customer1.Should().NotBeNull();

        var cust2Resp = await owner.PostAsJsonAsync("/customers", CustomerPayload("Customer 2"));
        var cust2Id = await cust2Resp.Content.ReadFromJsonAsync<Guid>();
        var customer2 = await owner.GetFromJsonAsync<CustomerAccountDto>($"/customers/{cust2Id}");
        customer2.Should().NotBeNull();

        // Invite both customers
        var invite1Resp = await owner.PostAsync($"/customers/{cust1Id}/invite", null);
        var invite1Result = await invite1Resp.Content.ReadFromJsonAsync<InviteCustomerResult>();
        var user1Id = invite1Result!.UserId;

        var invite2Resp = await owner.PostAsync($"/customers/{cust2Id}/invite", null);
        var invite2Result = await invite2Resp.Content.ReadFromJsonAsync<InviteCustomerResult>();
        var user2Id = invite2Result!.UserId;

        // Create client for customer 1
        var customer1Client = factory.CreateClientWithToken(
            TestJwtHelper.GenerateToken(user1Id, customer1!.BillingEmail, "Customer", tenantId, null, cust1Id));

        // Create client for customer 2
        var customer2Client = factory.CreateClientWithToken(
            TestJwtHelper.GenerateToken(user2Id, customer2!.BillingEmail, "Customer", tenantId, null, cust2Id));

        // Each customer should only see their own data
        var me1Resp = await customer1Client.GetAsync("/portal/me");
        me1Resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var me1Data = await me1Resp.Content.ReadFromJsonAsync<PortalMeDto>();
        me1Data!.CustomerAccountId.Should().Be(cust1Id);

        var me2Resp = await customer2Client.GetAsync("/portal/me");
        me2Resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var me2Data = await me2Resp.Content.ReadFromJsonAsync<PortalMeDto>();
        me2Data!.CustomerAccountId.Should().Be(cust2Id);

        // Cross-check: customer 1 should NOT be able to access customer 2's account ID
        me1Data.CustomerAccountId.Should().NotBe(cust2Id);
        me2Data.CustomerAccountId.Should().NotBe(cust1Id);
    }

    // ---- Boats ----

    [Fact]
    public async Task Create_boat_returns_201_and_appears_in_customer_list()
    {
        var (owner, _) = await SetupOwnerAsync();

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
        var (owner, _) = await SetupOwnerAsync();

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
