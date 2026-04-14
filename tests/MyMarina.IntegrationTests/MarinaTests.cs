using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MyMarina.Application.Marinas;
using MyMarina.Application.Tenants;
using MyMarina.Domain.Enums;

namespace MyMarina.IntegrationTests;

/// <summary>
/// Tests for GET/POST/PUT /marinas.
/// Creates a tenant via the platform client, then operates as the marina owner.
/// </summary>
public class MarinaTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _platformClient = factory.CreatePlatformOperatorClient();

    private async Task<(Guid TenantId, HttpClient OwnerClient)> CreateTenantAsync()
    {
        var slug = $"marina-{Guid.NewGuid():N}"[..28];
        var resp = await _platformClient.PostAsJsonAsync("/tenants", new
        {
            Name              = "Test Marina Co",
            Slug              = slug,
            OwnerEmail        = $"{Guid.NewGuid():N}@example.com",
            OwnerFirstName    = "Alice",
            OwnerLastName     = "Smith",
            OwnerPassword     = "OwnerPass@123",
            SubscriptionTier  = SubscriptionTier.Free,
        });
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<CreateTenantResult>();
        var client = factory.CreateMarinaOwnerClient(result!.TenantId);
        return (result.TenantId, client);
    }

    [Fact]
    public async Task Create_marina_returns_201_and_id()
    {
        var (_, ownerClient) = await CreateTenantAsync();

        var response = await ownerClient.PostAsJsonAsync("/marinas", new
        {
            Name        = "Sunrise Marina",
            Address     = new { Street = "1 Dock Rd", City = "Seaside", State = "FL", Zip = "33101", Country = "US" },
            PhoneNumber = "555-0100",
            Email       = "info@sunrise.io",
            TimeZoneId  = "America/New_York",
            Website     = (string?)null,
            Description = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Get_marinas_returns_only_own_tenant_records()
    {
        var (tenantId1, owner1) = await CreateTenantAsync();
        var (_, owner2)         = await CreateTenantAsync();

        // Owner1 creates a marina
        var createResp = await owner1.PostAsJsonAsync("/marinas", new
        {
            Name        = "Tenant1 Marina",
            Address     = new { Street = "2 Sea Lane", City = "Port", State = "FL", Zip = "33102", Country = "US" },
            PhoneNumber = "555-0101",
            Email       = "t1@marina.io",
            TimeZoneId  = "America/New_York",
            Website     = (string?)null,
            Description = (string?)null,
        });
        createResp.EnsureSuccessStatusCode();
        var marinaId = await createResp.Content.ReadFromJsonAsync<Guid>();

        // Owner1 can see their marina
        var list1 = await owner1.GetFromJsonAsync<IReadOnlyList<MarinaDto>>("/marinas");
        list1.Should().Contain(m => m.Id == marinaId);

        // Owner2 cannot see Owner1's marina (tenant isolation)
        var list2 = await owner2.GetFromJsonAsync<IReadOnlyList<MarinaDto>>("/marinas");
        list2.Should().NotContain(m => m.Id == marinaId);
    }

    [Fact]
    public async Task Get_marina_by_id_returns_correct_record()
    {
        var (_, ownerClient) = await CreateTenantAsync();

        var createResp = await ownerClient.PostAsJsonAsync("/marinas", new
        {
            Name        = "Detail Marina",
            Address     = new { Street = "3 Wave Blvd", City = "Coast", State = "CA", Zip = "90001", Country = "US" },
            PhoneNumber = "555-0102",
            Email       = "detail@marina.io",
            TimeZoneId  = "America/Los_Angeles",
            Website     = "https://detail.marina.io",
            Description = "A test marina",
        });
        var id = await createResp.Content.ReadFromJsonAsync<Guid>();

        var marina = await ownerClient.GetFromJsonAsync<MarinaDto>($"/marinas/{id}");
        marina.Should().NotBeNull();
        marina!.Name.Should().Be("Detail Marina");
        marina.Email.Should().Be("detail@marina.io");
        marina.Website.Should().Be("https://detail.marina.io");
    }

    [Fact]
    public async Task Update_marina_returns_204_and_persists_changes()
    {
        var (_, ownerClient) = await CreateTenantAsync();

        var createResp = await ownerClient.PostAsJsonAsync("/marinas", new
        {
            Name        = "Old Name",
            Address     = new { Street = "4 Bay St", City = "Harbor", State = "ME", Zip = "04101", Country = "US" },
            PhoneNumber = "555-0103",
            Email       = "old@marina.io",
            TimeZoneId  = "America/New_York",
            Website     = (string?)null,
            Description = (string?)null,
        });
        var id = await createResp.Content.ReadFromJsonAsync<Guid>();

        var updateResp = await ownerClient.PutAsJsonAsync($"/marinas/{id}", new
        {
            Name        = "New Name",
            Address     = new { Street = "4 Bay St", City = "Harbor", State = "ME", Zip = "04101", Country = "US" },
            PhoneNumber = "555-9999",
            Email       = "new@marina.io",
            TimeZoneId  = "America/New_York",
            Website     = (string?)null,
            Description = "Updated description",
        });

        updateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var marina = await ownerClient.GetFromJsonAsync<MarinaDto>($"/marinas/{id}");
        marina!.Name.Should().Be("New Name");
        marina.PhoneNumber.Should().Be("555-9999");
        marina.Description.Should().Be("Updated description");
    }
}
