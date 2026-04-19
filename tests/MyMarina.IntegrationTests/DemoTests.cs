using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyMarina.Domain.Enums;

namespace MyMarina.IntegrationTests;

/// <summary>
/// Tests for demo session provisioning, tier enforcement, and cleanup.
/// </summary>
public class DemoTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    // --- Task 1.5: Tier enforcement ---

    [Fact]
    public async Task Free_tier_token_calling_pro_gated_endpoint_returns_403_with_tier_required()
    {
        var tenantId = Guid.NewGuid();
        var freeClient = factory.CreateClientWithToken(
            TestJwtHelper.GenerateToken(Guid.NewGuid(), "owner@test.io", "TenantOwner",
                tenantId: tenantId, tier: SubscriptionTier.Free));

        // GET /demo/capabilities?tier=pro is gated at Pro tier
        var response = await freeClient.GetAsync("/demo/capabilities?tier=pro");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("error_code");
        body!["error_code"].ToString().Should().Be("tier_required");
    }

    [Fact]
    public async Task Pro_tier_token_calling_pro_gated_endpoint_returns_200()
    {
        var tenantId = Guid.NewGuid();
        var proClient = factory.CreateClientWithToken(
            TestJwtHelper.GenerateToken(Guid.NewGuid(), "owner@test.io", "TenantOwner",
                tenantId: tenantId, tier: SubscriptionTier.Pro));

        var response = await proClient.GetAsync("/demo/capabilities?tier=pro");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- Task 4.5: Demo session provisions a tenant ---

    [Fact]
    public async Task Post_demo_session_operator_returns_token_with_is_demo_and_tenant_claims()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync("/demo/session?role=operator", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DemoSessionResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();

        var claims = TestJwtHelper.ParseClaims(body.Token!);
        claims.Should().ContainKey("is_demo");
        claims["is_demo"].Should().Be("true");
        claims.Should().ContainKey("tenant_id");
        claims.Should().ContainKey("subscription_tier");
    }

    [Fact]
    public async Task Post_demo_session_customer_returns_token_scoped_to_customer_context()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync("/demo/session?role=customer", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DemoSessionResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();

        var claims = TestJwtHelper.ParseClaims(body.Token!);
        claims["is_demo"].Should().Be("true");
        claims.Should().ContainKey("customer_account_id");
        claims["role"].Should().Be("Customer");
    }

    [Fact]
    public async Task Post_demo_session_with_invalid_role_returns_400()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsync("/demo/session?role=admin", null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_demo_session_with_free_tier_blocks_pro_endpoint()
    {
        var client = factory.CreateClient();
        var sessionResponse = await client.PostAsync("/demo/session?role=operator&tier=free", null);
        sessionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await sessionResponse.Content.ReadFromJsonAsync<DemoSessionResponse>();
        var demoClient = factory.CreateClientWithToken(body!.Token!);

        var response = await demoClient.GetAsync("/demo/capabilities?tier=pro");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- Task 4.7: Expired tenants cleaned up ---

    [Fact]
    public async Task Delete_expired_demo_tenants_removes_demo_tenant_data()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyMarina.Infrastructure.Persistence.AppDbContext>();

        // Insert a demo tenant that expired 1 minute ago
        var expiredTenant = new MyMarina.Domain.Entities.Tenant
        {
            Id = Guid.CreateVersion7(),
            Name = "Expired Demo",
            Slug = $"expired-demo-{Guid.NewGuid():N}",
            IsDemo = true,
            DemoExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        };
        db.Tenants.Add(expiredTenant);
        await db.SaveChangesAsync();

        var handler = scope.ServiceProvider
            .GetRequiredService<MyMarina.Application.Abstractions.ICommandHandler<
                MyMarina.Application.Demo.DeleteExpiredDemoTenantsCommand>>();
        await handler.HandleAsync(new MyMarina.Application.Demo.DeleteExpiredDemoTenantsCommand());

        var stillExists = await db.Tenants.FindAsync(expiredTenant.Id);
        stillExists.Should().BeNull();
    }

    private record DemoSessionResponse(string? Token, DateTimeOffset ExpiresAt);
}
