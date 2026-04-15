using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Staff;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("staff")]
[Authorize(Roles = nameof(UserRole.MarinaOwner))]
public class StaffController(
    ICommandHandler<InviteStaffCommand, InviteStaffResult> inviteHandler) : ControllerBase
{
    [HttpPost("invite")]
    [ProducesResponseType(typeof(InviteStaffResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Invite([FromBody] InviteStaffCommand command, CancellationToken ct)
    {
        try
        {
            var result = await inviteHandler.HandleAsync(command, ct);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message.Contains("already exists")
                ? Conflict(new { message = ex.Message })
                : BadRequest(new { message = ex.Message });
        }
    }
}
