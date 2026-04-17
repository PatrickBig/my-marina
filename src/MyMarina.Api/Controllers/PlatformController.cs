using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Platform;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("platform")]
[Authorize(Roles = "PlatformAdmin")]
public class PlatformController(
    IQueryHandler<GetPlatformUsersQuery, IReadOnlyList<PlatformUserDto>> getUsersHandler,
    IQueryHandler<GetPlatformUserQuery, PlatformUserDto?> getUserHandler,
    ICommandHandler<ResetUserPasswordCommand> resetPasswordHandler,
    ICommandHandler<DeactivateUserCommand> deactivateHandler,
    ICommandHandler<ReactivateUserCommand> reactivateHandler,
    IQueryHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>> getAuditLogsHandler) : ControllerBase
{
    // ── Users ─────────────────────────────────────────────────────────────────

    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<PlatformUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] Guid? tenantId,
        [FromQuery] UserRole? role,
        CancellationToken ct = default)
    {
        var users = await getUsersHandler.HandleAsync(
            new GetPlatformUsersQuery(search, tenantId, role), ct);
        return Ok(users);
    }

    [HttpGet("users/{id:guid}")]
    [ProducesResponseType(typeof(PlatformUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        var user = await getUserHandler.HandleAsync(new GetPlatformUserQuery(id), ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost("users/{id:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        Guid id, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        try
        {
            await resetPasswordHandler.HandleAsync(new ResetUserPasswordCommand(id, request.NewPassword), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("users/{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        try
        {
            await deactivateHandler.HandleAsync(new DeactivateUserCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("users/{id:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct)
    {
        try
        {
            await reactivateHandler.HandleAsync(new ReactivateUserCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── Audit log ─────────────────────────────────────────────────────────────

    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? userId,
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await getAuditLogsHandler.HandleAsync(
            new GetAuditLogsQuery(tenantId, userId, action, entityType, from, to, page, pageSize), ct);
        return Ok(result);
    }
}

// ── Request models ────────────────────────────────────────────────────────────

public sealed record ResetPasswordRequest(string NewPassword);
