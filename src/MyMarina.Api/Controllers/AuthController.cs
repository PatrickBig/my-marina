using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Auth;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    ICommandHandler<LoginCommand, LoginResult> loginHandler,
    ICommandHandler<ChooseContextCommand, ContextToken> chooseContextHandler) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        try
        {
            var result = await loginHandler.HandleAsync(command, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("choose-context")]
    [ProducesResponseType(typeof(ContextToken), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChooseContext([FromBody] ChooseContextCommand command, CancellationToken ct)
    {
        try
        {
            var result = await chooseContextHandler.HandleAsync(command, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
