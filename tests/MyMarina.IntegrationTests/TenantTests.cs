using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MyMarina.Application.Tenants;
using MyMarina.Domain.Enums;

namespace MyMarina.IntegrationTests;

/// <summary>
/// Tests for GET /tenants and POST /tenants (platform-operator-only).
/// </summary>
public class TenantTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _platformClient = factory.CreatePlatformOperatorClient();
    private readonly HttpClient _anonClient     = factory.CreateClient();

    [Fact]
    public async Task Get_tenants_as_platform_operator_returns_200()
    {
        var response = await _platformClient.GetAsync("/tenants");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_tenants_without_auth_returns_401()
    {
        var response = await _anonClient.GetAsync("/tenants");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_tenant_returns_201_and_contains_tenant_in_list()
    {
        var slug = $"test-marina-{Guid.NewGuid():N}"[..30];

        var response = await _platformClient.PostAsJsonAsync("/tenants", new
        {
            Name              = "Test Marina Corp",
            Slug              = slug,
            OwnerEmail        = $"{slug}@example.com",
            OwnerFirstName    = "Jane",
            OwnerLastName     = "Doe",
            OwnerPassword     = "SecureOwner@123",
            SubscriptionTier  = SubscriptionTier.Free,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateTenantResult>();
        result.Should().NotBeNull();
        result!.TenantId.Should().NotBe(Guid.Empty);
        result.OwnerId.Should().NotBe(Guid.Empty);

        // The new tenant should appear in GET /tenants
        var list = await _platformClient.GetFromJsonAsync<IReadOnlyList<TenantDto>>("/tenants");
        list.Should().Contain(t => t.Id == result.TenantId);
    }

    [Fact]
    public async Task Create_tenant_with_duplicate_slug_returns_409()
    {
        var slug = $"dupe-{Guid.NewGuid():N}"[..28];

        async Task<HttpResponseMessage> CreateAsync() => await _platformClient.PostAsJsonAsync("/tenants", new
        {
            Name              = "Dup Marina",
            Slug              = slug,
            OwnerEmail        = $"{Guid.NewGuid():N}@example.com",
            OwnerFirstName    = "A",
            OwnerLastName     = "B",
            OwnerPassword     = "SecureOwner@123",
            SubscriptionTier  = SubscriptionTier.Free,
        });

        var first = await CreateAsync();
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await CreateAsync();
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_tenant_returns_204()
    {
        // Create a tenant first
        var slug = $"upd-{Guid.NewGuid():N}"[..28];
        var createResp = await _platformClient.PostAsJsonAsync("/tenants", new
        {
            Name              = "Update Me",
            Slug              = slug,
            OwnerEmail        = $"{Guid.NewGuid():N}@example.com",
            OwnerFirstName    = "A",
            OwnerLastName     = "B",
            OwnerPassword     = "SecureOwner@123",
            SubscriptionTier  = SubscriptionTier.Free,
        });
        var created = await createResp.Content.ReadFromJsonAsync<CreateTenantResult>();

        var updateResp = await _platformClient.PutAsJsonAsync($"/tenants/{created!.TenantId}", new
        {
            Name             = "Updated Name",
            IsActive         = false,
            SubscriptionTier = SubscriptionTier.Pro,
        });

        updateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await _platformClient.GetFromJsonAsync<IReadOnlyList<TenantDto>>("/tenants");
        var tenant = list!.First(t => t.Id == created.TenantId);
        tenant.Name.Should().Be("Updated Name");
        tenant.IsActive.Should().BeFalse();
        tenant.SubscriptionTier.Should().Be(SubscriptionTier.Pro);
    }
}
