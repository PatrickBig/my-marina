using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Platform;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("platform")]
[Authorize]
public class PlatformOperatorController(
    ICommandHandler<CreateTenantCommand, CreateTenantResponse> createTenant,
    ICommandHandler<SuspendTenantCommand> suspendTenant,
    ICommandHandler<ReactivateTenantCommand> reactivateTenant,
    ICommandHandler<ForceSignOutCommand> forceSignOut,
    ICommandHandler<DeactivateUserCommand> deactivateUser,
    ICommandHandler<ActivateUserCommand> activateUser,
    ICommandHandler<RemoveListingCommand> removeListing,
    IQueryHandler<GetTenantsQuery, PagedResult<TenantSummaryDto>> getTenants,
    IQueryHandler<SearchUsersQuery, PagedResult<UserSummaryDto>> searchUsers,
    IQueryHandler<GetAuditLogQuery, PagedResult<AuditLogEntryDto>> getAuditLog,
    IQueryHandler<GetListingModerationQueueQuery, PagedResult<ListingModerationDto>> getListings,
    IUserContext userContext)
    : ControllerBase
{
    // ── Guard ────────────────────────────────────────────────────────────────

    private IActionResult? RequireOperator() =>
        userContext.IsPlatformOperator ? null : Forbid();

    // ── Tenants ──────────────────────────────────────────────────────────────

    [HttpGet("tenants")]
    [ProducesResponseType(typeof(PagedResult<TenantSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenants(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        if (RequireOperator() is { } r) return r;
        return Ok(await getTenants.HandleAsync(new GetTenantsQuery(search, page, pageSize), ct));
    }

    [HttpPost("tenants")]
    [ProducesResponseType(typeof(CreateTenantResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantBody body, CancellationToken ct)
    {
        if (RequireOperator() is { } r) return r;
        var result = await createTenant.HandleAsync(
            new CreateTenantCommand(body.Name, body.Slug, body.BillingEmail, body.SubscriptionTier ?? "Free"), ct);
        return CreatedAtAction(nameof(GetTenants), result);
    }

    [HttpPatch("tenants/{tenantId:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SuspendTenant(
        Guid tenantId, [FromBody] ReasonBody body, CancellationToken ct)
    {
        if (RequireOperator() is { } r) return r;
        try
        {
            await suspendTenant.HandleAsync(new SuspendTenantCommand(tenantId, body.Reason), ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPatch("tenants/{tenantId:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReactivateTenant(Guid tenantId, CancellationToken ct)
    {
        if (RequireOperator() is { } r) return r;
        try
        {
            await reactivateTenant.HandleAsync(new ReactivateTenantCommand(tenantId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    [HttpGet("users")]
    [ProducesResponseType(typeof(PagedResult<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        if (RequireOperator() is { } r) return r;
        return Ok(await searchUsers.HandleAsync(new SearchUsersQuery(q, page, pageSize), ct));
    }

    [HttpPost("users/{userId:guid}/force-signout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForceSignOut(Guid userId, CancellationToken ct)
    {
        if (RequireOperator() is { } r) return r;
        await forceSignOut.HandleAsync(new ForceSignOutCommand(userId), ct);
        return NoContent();
    }

    [HttpPatch("users/{userId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeactivateUser(
        Guid userId, [FromBody] ReasonBody body, CancellationToken ct)
    {
        if (RequireOperator() is { } r) return r;
        try
        {
            await deactivateUser.HandleAsync(new DeactivateUserCommand(userId, body.Reason), ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPatch("users/{userId:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ActivateUser(Guid userId, CancellationToken ct)
    {
        if (RequireOperator() is { } r) return r;
        try
        {
            await activateUser.HandleAsync(new ActivateUserCommand(userId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ── Audit log ─────────────────────────────────────────────────────────────

    [HttpGet("audit-log")]
    [ProducesResponseType(typeof(PagedResult<AuditLogEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] Guid? tenantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (RequireOperator() is { } r) return r;
        return Ok(await getAuditLog.HandleAsync(new GetAuditLogQuery(tenantId, page, pageSize), ct));
    }

    // ── Listing moderation ────────────────────────────────────────────────────

    [HttpGet("listings")]
    [ProducesResponseType(typeof(PagedResult<ListingModerationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetListings(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (RequireOperator() is { } r) return r;
        return Ok(await getListings.HandleAsync(new GetListingModerationQueueQuery(page, pageSize), ct));
    }

    [HttpDelete("listings/{listingId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveListing(
        Guid listingId, [FromBody] ReasonBody body, CancellationToken ct)
    {
        if (RequireOperator() is { } r) return r;
        try
        {
            await removeListing.HandleAsync(new RemoveListingCommand(listingId, body.Reason), ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ── Request bodies ────────────────────────────────────────────────────────

    public record CreateTenantBody(string Name, string Slug, string? BillingEmail, string? SubscriptionTier);
    public record ReasonBody(string? Reason);
}
