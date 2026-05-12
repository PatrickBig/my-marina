using System.Net;
using System.Net.Http.Json;
using MyMarina.Application.Identity;

namespace MyMarina.IntegrationTests;

[Collection("Integration")]
public class AuthTests(ApiWebApplicationFactory factory)
{
    [Fact]
    public async Task Register_ValidRequest_Returns204()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            email = $"new-{Guid.NewGuid():N}@example.com",
            password = "TestPass!word1",
            firstName = "Test",
            lastName = "User",
            marketingOptIn = false,
            termsAccepted = true,
        });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            email = ApiWebApplicationFactory.PlatformOperatorEmail,
            password = ApiWebApplicationFactory.PlatformOperatorPassword,
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body?.AccessToken);
        Assert.NotNull(body?.RefreshToken);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "nobody@example.com",
            password = "WrongPass!word1",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsProfile()
    {
        var client = factory.CreatePlatformOperatorClient();
        var response = await client.GetAsync("/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.Equal(ApiWebApplicationFactory.PlatformOperatorEmail, body?.Email);
    }

    [Fact]
    public async Task GetMe_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
