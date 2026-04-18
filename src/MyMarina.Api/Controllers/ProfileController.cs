using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Profile;
using MyMarina.Infrastructure.Profile;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("profile")]
[Authorize]
public class ProfileController(
    IQueryHandler<GetProfileQuery, GetProfileResult> getProfileHandler,
    ICommandHandler<UpdateProfileCommand> updateHandler,
    ICommandHandler<ChangeEmailCommand> changeEmailHandler,
    ICommandHandler<ChangePasswordCommand> changePasswordHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(GetProfileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var result = await getProfileHandler.HandleAsync(new GetProfileQuery(), ct);
        return Ok(result);
    }

    [HttpPut]
    [ProducesResponseType(typeof(GetProfileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileCommand command, CancellationToken ct)
    {
        try
        {
            await updateHandler.HandleAsync(command, ct);
            var result = await getProfileHandler.HandleAsync(new GetProfileQuery(), ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("change-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailCommand command, CancellationToken ct)
    {
        try
        {
            await changeEmailHandler.HandleAsync(command, ct);
            return Ok();
        }
        catch (EmailConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command, CancellationToken ct)
    {
        try
        {
            await changePasswordHandler.HandleAsync(command, ct);
            return Ok();
        }
        catch (PasswordChangeFailedException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
