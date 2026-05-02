using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Memberships;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Authorize]
public class MembershipsController(
    ICommandHandler<InviteStaffCommand, MembershipDto> inviteStaff,
    ICommandHandler<AcceptMembershipCommand> acceptMembership,
    ICommandHandler<UpdateMembershipRoleCommand, MembershipDto> updateRole,
    ICommandHandler<RevokeMembershipCommand> revokeMembership,
    IQueryHandler<GetMarinaStaffQuery, IReadOnlyList<MembershipDto>> getMarinaStaff,
    IQueryHandler<GetMyMembershipsQuery, IReadOnlyList<MembershipDto>> getMyMemberships,
    IUserContext userContext)
    : ControllerBase
{
    // GET /me/memberships — all memberships for the current user
    [HttpGet("me/memberships")]
    [ProducesResponseType(typeof(IReadOnlyList<MembershipDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyMemberships(CancellationToken ct)
    {
        var memberships = await getMyMemberships.HandleAsync(
            new GetMyMembershipsQuery(userContext.UserId!.Value), ct);
        return Ok(memberships);
    }

    // GET /marinas/{marinaId}/staff
    [HttpGet("marinas/{marinaId:guid}/staff")]
    [ProducesResponseType(typeof(IReadOnlyList<MembershipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStaff(Guid marinaId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();
        var staff = await getMarinaStaff.HandleAsync(new GetMarinaStaffQuery(marinaId, userContext.UserId!.Value), ct);
        return Ok(staff);
    }

    // POST /marinas/{marinaId}/staff/invite
    [HttpPost("marinas/{marinaId:guid}/staff/invite")]
    [ProducesResponseType(typeof(MembershipDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> InviteStaff(Guid marinaId, [FromBody] InviteStaffRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();

        if (!Enum.TryParse<MembershipRole>(request.Role, ignoreCase: true, out var role))
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["role"] = [$"Unknown role: {request.Role}"] }));

        var tenantId = userContext.Memberships
            .FirstOrDefault(m => m.MarinaId == marinaId)?.TenantId
            ?? throw new InvalidOperationException("Could not resolve tenant for marina.");

        var membership = await inviteStaff.HandleAsync(new InviteStaffCommand(
            MarinaId: marinaId,
            TenantId: tenantId,
            InvitedByUserId: userContext.UserId!.Value,
            Email: request.Email,
            Role: role), ct);

        return CreatedAtAction(nameof(GetStaff), new { marinaId }, membership);
    }

    // POST /memberships/{membershipId}/accept
    [HttpPost("memberships/{membershipId:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptMembership(Guid membershipId, CancellationToken ct)
    {
        try
        {
            await acceptMembership.HandleAsync(
                new AcceptMembershipCommand(membershipId, userContext.UserId!.Value), ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // PATCH /marinas/{marinaId}/staff/{membershipId}/role
    [HttpPatch("marinas/{marinaId:guid}/staff/{membershipId:guid}/role")]
    [ProducesResponseType(typeof(MembershipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(Guid marinaId, Guid membershipId, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Owner)) return Forbid();

        if (!Enum.TryParse<MembershipRole>(request.Role, ignoreCase: true, out var role))
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["role"] = [$"Unknown role: {request.Role}"] }));

        try
        {
            var membership = await updateRole.HandleAsync(
                new UpdateMembershipRoleCommand(membershipId, marinaId, userContext.UserId!.Value, role), ct);
            return Ok(membership);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // DELETE /marinas/{marinaId}/staff/{membershipId}
    [HttpDelete("marinas/{marinaId:guid}/staff/{membershipId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeMembership(Guid marinaId, Guid membershipId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();
        try
        {
            await revokeMembership.HandleAsync(
                new RevokeMembershipCommand(membershipId, marinaId, userContext.UserId!.Value), ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}

// ---------- request records ----------

public sealed record InviteStaffRequest(string Email, string Role);
public sealed record UpdateRoleRequest(string Role);
