using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MyMarina.Application.Auth;

namespace MyMarina.IntegrationTests;

/// <summary>
/// Tests for POST /auth/login.
/// Uses the platform operator seeded by ApiWebApplicationFactory so that
/// at least one real user exists in the DB.
/// </summary>
public class AuthTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_with_valid_credentials_returns_200_and_token()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            Email    = ApiWebApplicationFactory.PlatformOperatorEmail,
            Password = ApiWebApplicationFactory.PlatformOperatorPassword,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<LoginResult>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.Email.Should().Be(ApiWebApplicationFactory.PlatformOperatorEmail);
        body.Role.Should().Be(MyMarina.Domain.Enums.UserRole.PlatformOperator);
        body.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            Email    = ApiWebApplicationFactory.PlatformOperatorEmail,
            Password = "WrongPassword!99",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_with_unknown_email_returns_401()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            Email    = "nobody@nowhere.io",
            Password = "whatever",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Accessing_protected_endpoint_without_token_returns_401()
    {
        var response = await _client.GetAsync("/tenants");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
