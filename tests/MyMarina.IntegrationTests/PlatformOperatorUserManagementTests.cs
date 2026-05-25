using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyMarina.Application.Platform;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.IntegrationTests;

[Collection("Integration")]
public class PlatformOperatorUserManagementTests(ApiWebApplicationFactory factory) : IAsyncLifetime
{
    private HttpClient _operatorClient = null!;
    private HttpClient _regularClient = null!;
    private ApplicationUser _testUser = null!;
    private ApplicationUser _platformOperator = null!;

    public async Task InitializeAsync()
    {
        _operatorClient = factory.CreatePlatformOperatorClient();

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Get the platform operator user
        _platformOperator = (await userManager.FindByEmailAsync(ApiWebApplicationFactory.PlatformOperatorEmail))!;

        // Create a test user
        _testUser = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            Email = $"testuser-{Guid.NewGuid():N}@example.com",
            UserName = $"testuser-{Guid.NewGuid():N}@example.com",
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true,
            IsActive = true,
        };
        var result = await userManager.CreateAsync(_testUser, "TestPassword!123");
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        // Create a regular user client
        _regularClient = factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await userManager.DeleteAsync(_testUser);
    }

    #region Search Users Tests

    [Fact]
    public async Task SearchUsers_ByEmail_ReturnsUser()
    {
        var response = await _operatorClient.GetAsync($"/platform/users?q={_testUser.Email}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<UserSummaryDto>>();
        Assert.NotNull(result);
        Assert.Contains(result.Items, u => u.Email == _testUser.Email);
    }

    [Fact]
    public async Task SearchUsers_ByFirstName_ReturnsUser()
    {
        var response = await _operatorClient.GetAsync($"/platform/users?q=Test");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<UserSummaryDto>>();
        Assert.NotNull(result);
        Assert.Contains(result.Items, u => u.FirstName == "Test");
    }

    [Fact]
    public async Task SearchUsers_ByLastName_ReturnsUser()
    {
        var response = await _operatorClient.GetAsync($"/platform/users?q=User");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<UserSummaryDto>>();
        Assert.NotNull(result);
        Assert.Contains(result.Items, u => u.LastName == "User");
    }

    [Fact]
    public async Task SearchUsers_NonOperator_Returns401()
    {
        var response = await _regularClient.GetAsync($"/platform/users?q=test");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Get User Profile Tests

    [Fact]
    public async Task GetUserProfile_ValidUser_ReturnsProfile()
    {
        var response = await _operatorClient.GetAsync($"/platform/users/{_testUser.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.NotNull(profile);
        Assert.Equal(_testUser.Email, profile.User.Email);
        Assert.Equal(_testUser.FirstName, profile.User.FirstName);
        Assert.Equal(_testUser.LastName, profile.User.LastName);
        Assert.NotNull(profile.Vessels);
        Assert.NotNull(profile.Reservations);
        Assert.NotNull(profile.Memberships);
        Assert.NotNull(profile.RecentActivity);
    }

    [Fact]
    public async Task GetUserProfile_InvalidUser_Returns404()
    {
        var invalidUserId = Guid.NewGuid();
        var response = await _operatorClient.GetAsync($"/platform/users/{invalidUserId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUserProfile_NonOperator_Returns401()
    {
        var response = await _regularClient.GetAsync($"/platform/users/{_testUser.Id}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Change Email Tests

    [Fact]
    public async Task ChangeUserEmail_ValidEmail_Success()
    {
        var newEmail = $"newemail-{Guid.NewGuid():N}@example.com";
        var response = await _operatorClient.PatchAsJsonAsync($"/platform/users/{_testUser.Id}/email", new { newEmail });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify email was changed
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var updatedUser = await userManager.FindByIdAsync(_testUser.Id.ToString());
        Assert.NotNull(updatedUser);
        Assert.Equal(newEmail, updatedUser.Email);
        Assert.True(updatedUser.EmailConfirmed);
    }

    [Fact]
    public async Task ChangeUserEmail_DuplicateEmail_ReturnsBadRequest()
    {
        var response = await _operatorClient.PatchAsJsonAsync($"/platform/users/{_testUser.Id}/email",
            new { newEmail = ApiWebApplicationFactory.PlatformOperatorEmail });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangeUserEmail_InvalidUser_Returns404()
    {
        var invalidUserId = Guid.NewGuid();
        var response = await _operatorClient.PatchAsJsonAsync($"/platform/users/{invalidUserId}/email",
            new { newEmail = $"test-{Guid.NewGuid():N}@example.com" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangeUserEmail_NonOperator_Returns401()
    {
        var response = await _regularClient.PatchAsJsonAsync($"/platform/users/{_testUser.Id}/email",
            new { newEmail = $"test-{Guid.NewGuid():N}@example.com" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Change Name Tests

    [Fact]
    public async Task ChangeUserName_FirstNameOnly_Success()
    {
        var response = await _operatorClient.PatchAsJsonAsync($"/platform/users/{_testUser.Id}/name",
            new { firstName = "NewFirstName", lastName = (string?)null });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var updatedUser = await userManager.FindByIdAsync(_testUser.Id.ToString());
        Assert.NotNull(updatedUser);
        Assert.Equal("NewFirstName", updatedUser.FirstName);
        Assert.Equal("User", updatedUser.LastName); // unchanged
    }

    [Fact]
    public async Task ChangeUserName_LastNameOnly_Success()
    {
        var response = await _operatorClient.PatchAsJsonAsync($"/platform/users/{_testUser.Id}/name",
            new { firstName = (string?)null, lastName = "NewLastName" });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var updatedUser = await userManager.FindByIdAsync(_testUser.Id.ToString());
        Assert.NotNull(updatedUser);
        Assert.Equal("Test", updatedUser.FirstName); // unchanged
        Assert.Equal("NewLastName", updatedUser.LastName);
    }

    [Fact]
    public async Task ChangeUserName_BothNames_Success()
    {
        var response = await _operatorClient.PatchAsJsonAsync($"/platform/users/{_testUser.Id}/name",
            new { firstName = "NewFirst", lastName = "NewLast" });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var updatedUser = await userManager.FindByIdAsync(_testUser.Id.ToString());
        Assert.NotNull(updatedUser);
        Assert.Equal("NewFirst", updatedUser.FirstName);
        Assert.Equal("NewLast", updatedUser.LastName);
    }

    [Fact]
    public async Task ChangeUserName_NoFieldsProvided_ReturnsBadRequest()
    {
        var response = await _operatorClient.PatchAsJsonAsync($"/platform/users/{_testUser.Id}/name",
            new { firstName = (string?)null, lastName = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangeUserName_InvalidUser_Returns404()
    {
        var invalidUserId = Guid.NewGuid();
        var response = await _operatorClient.PatchAsJsonAsync($"/platform/users/{invalidUserId}/name",
            new { firstName = "New", lastName = (string?)null });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangeUserName_NonOperator_Returns401()
    {
        var response = await _regularClient.PatchAsJsonAsync($"/platform/users/{_testUser.Id}/name",
            new { firstName = "New", lastName = (string?)null });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Password Reset Tests

    [Fact]
    public async Task InitiatePasswordReset_ValidUser_Success()
    {
        var response = await _operatorClient.PostAsync($"/platform/users/{_testUser.Id}/password-reset", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var activeTokens = await db.RefreshTokens
            .Where(t => t.UserId == _testUser.Id && t.RevokedAt == null)
            .CountAsync();
        Assert.Equal(0, activeTokens);
    }

    [Fact]
    public async Task InitiatePasswordReset_InvalidUser_Returns404()
    {
        var invalidUserId = Guid.NewGuid();
        var response = await _operatorClient.PostAsync($"/platform/users/{invalidUserId}/password-reset", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task InitiatePasswordReset_NonOperator_Returns401()
    {
        var response = await _regularClient.PostAsync($"/platform/users/{_testUser.Id}/password-reset", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Audit Log Tests

    [Fact]
    public async Task ChangeUserEmail_CreatesAuditLog()
    {
        var newEmail = $"auditlog-{Guid.NewGuid():N}@example.com";
        await _operatorClient.PatchAsJsonAsync($"/platform/users/{_testUser.Id}/email", new { newEmail });

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditEntry = await db.AuditLogs
            .Where(a => a.TargetType == "User" && a.TargetId == _testUser.Id.ToString() && a.Action == "user.email_changed")
            .OrderByDescending(a => a.OccurredAt)
            .FirstOrDefaultAsync();

        Assert.NotNull(auditEntry);
        Assert.Equal(_platformOperator.Id, auditEntry.ActorUserId);
        Assert.Contains(newEmail, auditEntry.Details ?? "");
        Assert.DoesNotContain("Password", auditEntry.Details ?? ""); // No sensitive data
    }

    [Fact]
    public async Task ChangeUserName_CreatesAuditLog()
    {
        await _operatorClient.PatchAsJsonAsync($"/platform/users/{_testUser.Id}/name",
            new { firstName = "AuditTest", lastName = (string?)null });

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditEntry = await db.AuditLogs
            .Where(a => a.TargetType == "User" && a.TargetId == _testUser.Id.ToString() && a.Action == "user.first_name_changed")
            .OrderByDescending(a => a.OccurredAt)
            .FirstOrDefaultAsync();

        Assert.NotNull(auditEntry);
        Assert.Equal(_platformOperator.Id, auditEntry.ActorUserId);
        Assert.Contains("AuditTest", auditEntry.Details ?? "");
    }

    [Fact]
    public async Task InitiatePasswordReset_CreatesAuditLog()
    {
        await _operatorClient.PostAsync($"/platform/users/{_testUser.Id}/password-reset", null);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditEntry = await db.AuditLogs
            .Where(a => a.TargetType == "User" && a.TargetId == _testUser.Id.ToString() && a.Action == "user.password_reset_requested")
            .OrderByDescending(a => a.OccurredAt)
            .FirstOrDefaultAsync();

        Assert.NotNull(auditEntry);
        Assert.Equal(_platformOperator.Id, auditEntry.ActorUserId);
        Assert.DoesNotContain("password", auditEntry.Details?.ToLower() ?? ""); // No password/token in log
    }

    #endregion
}
