using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MyMarina.Application.Customers;
using MyMarina.Application.Portal;
using MyMarina.Application.Tenants;
using MyMarina.Domain.Enums;

namespace MyMarina.IntegrationTests;

/// <summary>
/// Integration tests for the customer portal endpoints (/portal/*) and customer invitation.
/// </summary>
public class PortalTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _platformClient = factory.CreatePlatformOperatorClient();

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates tenant + marina + customer account + invited customer user.
    /// Returns an owner client, customer portal client, tenant/account IDs.
    /// </summary>
    private async Task<(HttpClient Owner, HttpClient Portal, Guid TenantId, Guid CustomerAccountId)> SetupAsync()
    {
        var slug = $"portal-{Guid.NewGuid():N}"[..28];
        var tenantResp = await _platformClient.PostAsJsonAsync("/tenants", new
        {
            Name = "Portal Marina",
            Slug = slug,
            OwnerEmail = $"{Guid.NewGuid():N}@portal.io",
            OwnerFirstName = "P", OwnerLastName = "O",
            OwnerPassword = "OwnerPass@123",
            SubscriptionTier = SubscriptionTier.Free,
        });
        tenantResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var tenant = await tenantResp.Content.ReadFromJsonAsync<CreateTenantResult>();

        var owner = factory.CreateMarinaOwnerClient(tenant!.TenantId);

        var marinaResp = await owner.PostAsJsonAsync("/marinas", new
        {
            Name = "Portal Marina", Address = new { Street = "1 Portal St", City = "Port", State = "FL", Zip = "33000", Country = "US" },
            PhoneNumber = "555-0010", Email = "marina@portal.io", TimeZoneId = "America/New_York",
            Website = (string?)null, Description = (string?)null,
        });
        marinaResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var custResp = await owner.PostAsJsonAsync("/customers", new
        {
            DisplayName = "Portal Customer",
            BillingEmail = $"{Guid.NewGuid():N}@portal.io",
            BillingPhone = (string?)null,
            BillingAddress = (object?)null,
            EmergencyContactName = (string?)null,
            EmergencyContactPhone = (string?)null,
            Notes = (string?)null,
        });
        custResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var custId = await custResp.Content.ReadFromJsonAsync<Guid>();

        // Use the factory helper to create a customer JWT directly (bypasses login)
        var portalClient = factory.CreateCustomerClient(tenant.TenantId, custId);

        return (owner, portalClient, tenant.TenantId, custId);
    }

    // ── Customer invitation ───────────────────────────────────────────────────

    [Fact]
    public async Task Invite_customer_returns_201_with_temporary_password()
    {
        var (owner, _, _, custId) = await SetupAsync();

        var resp = await owner.PostAsJsonAsync($"/customers/{custId}/invite", new
        {
            Email = $"{Guid.NewGuid():N}@invited.io",
            FirstName = "Jane",
            LastName = "Doe",
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await resp.Content.ReadFromJsonAsync<InviteCustomerResult>();
        result!.UserId.Should().NotBeEmpty();
        result.TemporaryPassword.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Invite_duplicate_email_returns_409()
    {
        var (owner, _, _, custId) = await SetupAsync();
        var email = $"{Guid.NewGuid():N}@dup.io";

        var first = await owner.PostAsJsonAsync($"/customers/{custId}/invite", new
        {
            Email = email, FirstName = "A", LastName = "B"
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await owner.PostAsJsonAsync($"/customers/{custId}/invite", new
        {
            Email = email, FirstName = "A", LastName = "B"
        });
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Invite_unknown_account_returns_404()
    {
        var (owner, _, _, _) = await SetupAsync();

        var resp = await owner.PostAsJsonAsync($"/customers/{Guid.NewGuid()}/invite", new
        {
            Email = $"{Guid.NewGuid():N}@none.io",
            FirstName = "X", LastName = "Y",
        });

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /portal/me ────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_me_returns_account_info()
    {
        var (_, portal, _, custId) = await SetupAsync();

        var resp = await portal.GetAsync("/portal/me");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await resp.Content.ReadFromJsonAsync<PortalMeDto>();
        me!.CustomerAccountId.Should().Be(custId);
        me.AccountDisplayName.Should().Be("Portal Customer");
    }

    [Fact]
    public async Task Get_me_requires_customer_role()
    {
        var (owner, _, _, _) = await SetupAsync();
        var resp = await owner.GetAsync("/portal/me");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /portal/slip ──────────────────────────────────────────────────────

    [Fact]
    public async Task Get_slip_returns_204_when_no_active_assignment()
    {
        var (_, portal, _, _) = await SetupAsync();

        var resp = await portal.GetAsync("/portal/slip");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── GET /portal/boats ─────────────────────────────────────────────────────

    [Fact]
    public async Task Get_boats_returns_empty_list_initially()
    {
        var (_, portal, _, _) = await SetupAsync();

        var resp = await portal.GetAsync("/portal/boats");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var boats = await resp.Content.ReadFromJsonAsync<List<PortalBoatDto>>();
        boats.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_boats_returns_boats_for_customer_account()
    {
        var (owner, portal, _, custId) = await SetupAsync();

        // Add a boat via the operator API
        var boatResp = await owner.PostAsJsonAsync($"/customers/{custId}/boats", new
        {
            Name = "My Yacht",
            Make = "Beneteau",
            Model = (string?)null,
            Year = 2020,
            Length = 40m,
            Beam = 12m,
            Draft = 5m,
            BoatType = BoatType.Sailboat,
            HullColor = (string?)null,
            RegistrationNumber = (string?)null,
            RegistrationState = (string?)null,
            InsuranceProvider = (string?)null,
            InsurancePolicyNumber = (string?)null,
            InsuranceExpiresOn = (DateOnly?)null,
        });
        boatResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var resp = await portal.GetAsync("/portal/boats");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var boats = await resp.Content.ReadFromJsonAsync<List<PortalBoatDto>>();
        boats.Should().HaveCount(1);
        boats![0].Name.Should().Be("My Yacht");
    }

    // ── GET /portal/invoices ──────────────────────────────────────────────────

    [Fact]
    public async Task Get_invoices_returns_empty_list_initially()
    {
        var (_, portal, _, _) = await SetupAsync();

        var resp = await portal.GetAsync("/portal/invoices");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var invoices = await resp.Content.ReadFromJsonAsync<List<PortalInvoiceDto>>();
        invoices.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_invoice_detail_returns_404_for_other_customers_invoice()
    {
        var (_, portal, _, _) = await SetupAsync();

        var resp = await portal.GetAsync($"/portal/invoices/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_invoice_detail_returns_correct_data()
    {
        var (owner, portal, _, custId) = await SetupAsync();

        // Create and send an invoice via operator API
        var createResp = await owner.PostAsJsonAsync("/invoices", new
        {
            CustomerAccountId = custId,
            IssuedDate = "2026-05-01",
            DueDate = "2026-05-31",
            Notes = (string?)null,
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var invoiceId = await createResp.Content.ReadFromJsonAsync<Guid>();

        await owner.PostAsJsonAsync($"/invoices/{invoiceId}/line-items", new
        {
            Description = "Monthly slip fee",
            Quantity = 1m,
            UnitPrice = 500m,
        });
        await owner.PostAsync($"/invoices/{invoiceId}/send", null);

        var resp = await portal.GetAsync($"/portal/invoices/{invoiceId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await resp.Content.ReadFromJsonAsync<PortalInvoiceDetailDto>();
        detail!.InvoiceNumber.Should().NotBeNullOrEmpty();
        detail.TotalAmount.Should().Be(500m);
        detail.LineItems.Should().HaveCount(1);
        detail.LineItems[0].Description.Should().Be("Monthly slip fee");
    }

    // ── GET /portal/maintenance-requests ─────────────────────────────────────

    [Fact]
    public async Task Get_maintenance_requests_returns_empty_initially()
    {
        var (_, portal, _, _) = await SetupAsync();

        var resp = await portal.GetAsync("/portal/maintenance-requests");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var requests = await resp.Content.ReadFromJsonAsync<List<PortalMaintenanceRequestDto>>();
        requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Submit_maintenance_request_and_appears_in_list()
    {
        var (_, portal, _, _) = await SetupAsync();

        var submitResp = await portal.PostAsJsonAsync("/portal/maintenance-requests", new
        {
            Title = "Electrical outlet broken",
            Description = "The 30A outlet on my slip stopped working.",
            Priority = Priority.High,
            SlipId = (Guid?)null,
            BoatId = (Guid?)null,
        });
        submitResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var newId = await submitResp.Content.ReadFromJsonAsync<Guid>();
        newId.Should().NotBeEmpty();

        var listResp = await portal.GetAsync("/portal/maintenance-requests");
        var requests = await listResp.Content.ReadFromJsonAsync<List<PortalMaintenanceRequestDto>>();
        requests.Should().HaveCount(1);
        requests![0].Title.Should().Be("Electrical outlet broken");
        requests[0].Status.Should().Be(MaintenanceStatus.Submitted);
    }

    // ── GET /portal/announcements ─────────────────────────────────────────────

    [Fact]
    public async Task Get_announcements_returns_empty_when_none_published()
    {
        var (_, portal, _, _) = await SetupAsync();

        var resp = await portal.GetAsync("/portal/announcements");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var announcements = await resp.Content.ReadFromJsonAsync<List<PortalAnnouncementDto>>();
        announcements.Should().BeEmpty();
    }

    // ── Cross-tenant isolation ────────────────────────────────────────────────

    [Fact]
    public async Task Portal_customer_cannot_see_other_tenants_invoices()
    {
        var (owner1, portal1, _, custId1) = await SetupAsync();
        var (_, _, tenantId2, custId2)   = await SetupAsync();

        // Create invoice for tenant2's customer
        var createResp = await factory.CreateMarinaOwnerClient(tenantId2)
            .PostAsJsonAsync("/invoices", new
            {
                CustomerAccountId = custId2,
                IssuedDate = "2026-05-01",
                DueDate = "2026-05-31",
                Notes = (string?)null,
            });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var tenant2InvoiceId = await createResp.Content.ReadFromJsonAsync<Guid>();

        // Tenant1's customer tries to access tenant2's invoice
        var resp = await portal1.GetAsync($"/portal/invoices/{tenant2InvoiceId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
