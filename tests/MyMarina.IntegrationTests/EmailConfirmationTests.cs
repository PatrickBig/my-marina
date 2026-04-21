using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyMarina.Application.Customers;
using MyMarina.Application.Tenants;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;

namespace MyMarina.IntegrationTests;

/// <summary>Variant of the factory with email confirmation enforcement enabled.</summary>
public class RequireEmailConfirmedFactory : ApiWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:RequireConfirmedEmail"] = "true",
            }));
    }
}

/// <summary>
/// Integration tests for email confirmation flow (GET /auth/confirm-email)
/// and confirmation state on invited users.
/// </summary>
public class EmailConfirmationTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _platformClient = factory.CreatePlatformOperatorClient();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(HttpClient Owner, Guid TenantId, Guid CustomerAccountId)> SetupTenantAndCustomerAsync()
    {
        var slug = $"email-{Guid.NewGuid():N}"[..28];
        var tenantResp = await _platformClient.PostAsJsonAsync("/tenants", new
        {
            Name = "Email Test Marina",
            Slug = slug,
            OwnerEmail = $"{Guid.NewGuid():N}@emailtest.io",
            OwnerFirstName = "E", OwnerLastName = "T",
            OwnerPassword = "OwnerPass@123",
            SubscriptionTier = SubscriptionTier.Free,
        });
        tenantResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var tenant = await tenantResp.Content.ReadFromJsonAsync<CreateTenantResult>();

        var owner = factory.CreateMarinaOwnerClient(tenant!.TenantId);

        await owner.PostAsJsonAsync("/marinas", new
        {
            Name = "Email Test Marina",
            Address = new { Street = "1 Email St", City = "Port", State = "FL", Zip = "33000", Country = "US" },
            PhoneNumber = "555-0020", Email = "marina@emailtest.io", TimeZoneId = "America/New_York",
            Website = (string?)null, Description = (string?)null,
        });

        var custResp = await owner.PostAsJsonAsync("/customers", new
        {
            DisplayName = "Email Customer",
            BillingEmail = $"{Guid.NewGuid():N}@emailtest.io",
            BillingPhone = (string?)null,
            BillingAddress = (object?)null,
            EmergencyContactName = (string?)null,
            EmergencyContactPhone = (string?)null,
            Notes = (string?)null,
        });
        custResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var custId = await custResp.Content.ReadFromJsonAsync<Guid>();

        return (owner, tenant.TenantId, custId);
    }

    private async Task<(string UserId, string Token)> InviteAndGetConfirmationTokenAsync(HttpClient owner, Guid custId)
    {
        var inviteResp = await owner.PostAsJsonAsync($"/customers/{custId}/invite", new { });
        inviteResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var invite = await inviteResp.Content.ReadFromJsonAsync<InviteCustomerResult>();

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(invite!.UserId.ToString());
        user.Should().NotBeNull();

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user!);
        return (invite.UserId.ToString(), token);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Invite_customer_creates_user_with_email_unconfirmed()
    {
        var (owner, _, custId) = await SetupTenantAndCustomerAsync();

        var inviteResp = await owner.PostAsJsonAsync($"/customers/{custId}/invite", new { });
        inviteResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var invite = await inviteResp.Content.ReadFromJsonAsync<InviteCustomerResult>();

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(invite!.UserId.ToString());

        user.Should().NotBeNull();
        user!.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task Confirm_email_with_valid_token_returns_200_and_sets_confirmed()
    {
        var (owner, _, custId) = await SetupTenantAndCustomerAsync();
        var (userId, token) = await InviteAndGetConfirmationTokenAsync(owner, custId);

        var client = factory.CreateClient();
        var encodedToken = Uri.EscapeDataString(token);
        var resp = await client.GetAsync($"/auth/confirm-email?userId={userId}&token={encodedToken}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        user!.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task Confirm_email_with_invalid_token_returns_400()
    {
        var (owner, _, custId) = await SetupTenantAndCustomerAsync();
        var (userId, _) = await InviteAndGetConfirmationTokenAsync(owner, custId);

        var client = factory.CreateClient();
        var resp = await client.GetAsync($"/auth/confirm-email?userId={userId}&token=invalid-token");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Confirm_email_for_already_confirmed_user_returns_200()
    {
        var (owner, _, custId) = await SetupTenantAndCustomerAsync();
        var (userId, token) = await InviteAndGetConfirmationTokenAsync(owner, custId);

        var client = factory.CreateClient();
        var encodedToken = Uri.EscapeDataString(token);

        // First confirmation
        await client.GetAsync($"/auth/confirm-email?userId={userId}&token={encodedToken}");

        // Second confirmation — should be idempotent
        var resp = await client.GetAsync($"/auth/confirm-email?userId={userId}&token={encodedToken}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

/// <summary>
/// Tests that unconfirmed customers cannot log in when RequireConfirmedEmail is true.
/// Uses a dedicated factory with that setting enabled.
/// </summary>
public class RequireConfirmedEmailLoginTests(RequireEmailConfirmedFactory factory)
    : IClassFixture<RequireEmailConfirmedFactory>
{
    [Fact]
    public async Task Unconfirmed_customer_login_is_blocked_when_enforcement_enabled()
    {
        var platformClient = factory.CreatePlatformOperatorClient();

        // Provision tenant + marina + customer
        var slug = $"reqconf-{Guid.NewGuid():N}"[..28];
        var tenantResp = await platformClient.PostAsJsonAsync("/tenants", new
        {
            Name = "Confirmed Marina", Slug = slug,
            OwnerEmail = $"{Guid.NewGuid():N}@conf.io",
            OwnerFirstName = "C", OwnerLastName = "O",
            OwnerPassword = "OwnerPass@123",
            SubscriptionTier = SubscriptionTier.Free,
        });
        tenantResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var tenant = await tenantResp.Content.ReadFromJsonAsync<CreateTenantResult>();

        var owner = factory.CreateMarinaOwnerClient(tenant!.TenantId);
        await owner.PostAsJsonAsync("/marinas", new
        {
            Name = "Confirmed Marina",
            Address = new { Street = "1 St", City = "Port", State = "FL", Zip = "33000", Country = "US" },
            PhoneNumber = "555-0030", Email = "m@conf.io", TimeZoneId = "America/New_York",
            Website = (string?)null, Description = (string?)null,
        });

        var email = $"{Guid.NewGuid():N}@conf.io";
        var custResp = await owner.PostAsJsonAsync("/customers", new
        {
            DisplayName = "Unconfirmed Customer", BillingEmail = email,
            BillingPhone = (string?)null, BillingAddress = (object?)null,
            EmergencyContactName = (string?)null, EmergencyContactPhone = (string?)null,
            Notes = (string?)null,
        });
        custResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var custId = await custResp.Content.ReadFromJsonAsync<Guid>();

        var inviteResp = await owner.PostAsJsonAsync($"/customers/{custId}/invite", new { });
        inviteResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var invite = await inviteResp.Content.ReadFromJsonAsync<InviteCustomerResult>();

        // Attempt login without confirming email
        var client = factory.CreateClient();
        var loginResp = await client.PostAsJsonAsync("/auth/login", new
        {
            Email = email,
            Password = invite!.TemporaryPassword,
        });

        loginResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
