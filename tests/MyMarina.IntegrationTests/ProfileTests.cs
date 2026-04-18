using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MyMarina.Application.Auth;
using MyMarina.Application.Profile;

namespace MyMarina.IntegrationTests;

/// <summary>
/// Integration tests for GET/PUT /profile, POST /profile/change-email, POST /profile/change-password.
/// Uses the seeded platform operator as the authenticated user.
/// Email/password change tests go through the real login endpoint so the handler has
/// an actual database-backed user to look up.
/// </summary>
public class ProfileTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    // ── GET /profile ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfile_authenticated_returns_200_with_user_data()
    {
        var client = PlatformClient();

        var response = await client.GetAsync("/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetProfileResult>();
        result.Should().NotBeNull();
        result!.Email.Should().Be(ApiWebApplicationFactory.PlatformOperatorEmail);
        result.FirstName.Should().Be("Platform");
        result.LastName.Should().Be("Admin");
    }

    [Fact]
    public async Task GetProfile_unauthenticated_returns_401()
    {
        var response = await factory.CreateClient().GetAsync("/profile");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── PUT /profile ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfile_valid_data_returns_200_and_updated_values()
    {
        var client = PlatformClient();

        var response = await client.PutAsJsonAsync("/profile", new
        {
            FirstName = "UpdatedFirst",
            LastName = "UpdatedLast",
            PhoneNumber = "555-0001",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetProfileResult>();
        result!.FirstName.Should().Be("UpdatedFirst");
        result.LastName.Should().Be("UpdatedLast");
        result.PhoneNumber.Should().Be("555-0001");

        // Restore
        await client.PutAsJsonAsync("/profile", new
        {
            FirstName = "Platform",
            LastName = "Admin",
            PhoneNumber = (string?)null,
        });
    }

    [Fact]
    public async Task UpdateProfile_empty_first_name_returns_400()
    {
        var client = PlatformClient();

        var response = await client.PutAsJsonAsync("/profile", new
        {
            FirstName = "",
            LastName = "Admin",
            PhoneNumber = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /profile/change-email ────────────────────────────────────────────

    [Fact]
    public async Task ChangeEmail_correct_password_returns_200()
    {
        var newEmail = $"changed-{Guid.NewGuid():N}@mymarina.org";
        var token = await LoginAsync(ApiWebApplicationFactory.PlatformOperatorEmail, ApiWebApplicationFactory.PlatformOperatorPassword);
        var client = factory.CreateClientWithToken(token);

        var response = await client.PostAsJsonAsync("/profile/change-email", new
        {
            NewEmail = newEmail,
            CurrentPassword = ApiWebApplicationFactory.PlatformOperatorPassword,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Restore original email
        var restoreToken = await LoginAsync(newEmail, ApiWebApplicationFactory.PlatformOperatorPassword);
        var restoreClient = factory.CreateClientWithToken(restoreToken);
        await restoreClient.PostAsJsonAsync("/profile/change-email", new
        {
            NewEmail = ApiWebApplicationFactory.PlatformOperatorEmail,
            CurrentPassword = ApiWebApplicationFactory.PlatformOperatorPassword,
        });
    }

    [Fact]
    public async Task ChangeEmail_wrong_password_returns_400()
    {
        var token = await LoginAsync(ApiWebApplicationFactory.PlatformOperatorEmail, ApiWebApplicationFactory.PlatformOperatorPassword);
        var client = factory.CreateClientWithToken(token);

        var response = await client.PostAsJsonAsync("/profile/change-email", new
        {
            NewEmail = $"any-{Guid.NewGuid():N}@new.com",
            CurrentPassword = "WrongPassword!99",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangeEmail_same_email_same_user_returns_200()
    {
        // Changing to own current email (same user ID) is allowed — not a conflict
        var token = await LoginAsync(ApiWebApplicationFactory.PlatformOperatorEmail, ApiWebApplicationFactory.PlatformOperatorPassword);
        var client = factory.CreateClientWithToken(token);

        var response = await client.PostAsJsonAsync("/profile/change-email", new
        {
            NewEmail = ApiWebApplicationFactory.PlatformOperatorEmail,
            CurrentPassword = ApiWebApplicationFactory.PlatformOperatorPassword,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /profile/change-password ─────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_correct_current_password_returns_200()
    {
        const string newPassword = "NewAdmin@Marina456!";
        var token = await LoginAsync(ApiWebApplicationFactory.PlatformOperatorEmail, ApiWebApplicationFactory.PlatformOperatorPassword);
        var client = factory.CreateClientWithToken(token);

        var response = await client.PostAsJsonAsync("/profile/change-password", new
        {
            CurrentPassword = ApiWebApplicationFactory.PlatformOperatorPassword,
            NewPassword = newPassword,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Restore original password
        var restoreToken = await LoginAsync(ApiWebApplicationFactory.PlatformOperatorEmail, newPassword);
        var restoreClient = factory.CreateClientWithToken(restoreToken);
        await restoreClient.PostAsJsonAsync("/profile/change-password", new
        {
            CurrentPassword = newPassword,
            NewPassword = ApiWebApplicationFactory.PlatformOperatorPassword,
        });
    }

    [Fact]
    public async Task ChangePassword_wrong_current_password_returns_400()
    {
        var token = await LoginAsync(ApiWebApplicationFactory.PlatformOperatorEmail, ApiWebApplicationFactory.PlatformOperatorPassword);
        var client = factory.CreateClientWithToken(token);

        var response = await client.PostAsJsonAsync("/profile/change-password", new
        {
            CurrentPassword = "WrongPassword!99",
            NewPassword = "NewAdmin@Marina456!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_same_as_current_returns_400()
    {
        var token = await LoginAsync(ApiWebApplicationFactory.PlatformOperatorEmail, ApiWebApplicationFactory.PlatformOperatorPassword);
        var client = factory.CreateClientWithToken(token);

        var response = await client.PostAsJsonAsync("/profile/change-password", new
        {
            CurrentPassword = ApiWebApplicationFactory.PlatformOperatorPassword,
            NewPassword = ApiWebApplicationFactory.PlatformOperatorPassword,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private HttpClient PlatformClient()
        => factory.CreateClientWithToken(
            TestJwtHelper.GenerateToken(factory.PlatformOperatorId, ApiWebApplicationFactory.PlatformOperatorEmail, "PlatformAdmin"));

    private async Task<string> LoginAsync(string email, string password)
    {
        var resp = await factory.CreateClient().PostAsJsonAsync("/auth/login", new { Email = email, Password = password });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<LoginResult>();
        return body!.Token!;
    }
}
