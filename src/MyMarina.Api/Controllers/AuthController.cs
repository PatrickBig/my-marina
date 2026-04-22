using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Auth;
using MyMarina.Infrastructure.Email;
using MyMarina.Infrastructure.Identity;
using System.Security.Claims;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    ICommandHandler<LoginCommand, LoginResult> loginHandler,
    ICommandHandler<ChooseContextCommand, ContextToken> chooseContextHandler,
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    IOptions<EmailOptions> emailOptions) : ControllerBase
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

    [HttpGet("confirm-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return BadRequest(new { message = "Invalid confirmation link." });

        if (user.EmailConfirmed)
            return Ok(new { message = "Email already confirmed." });

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            return BadRequest(new { message = "Confirmation link is invalid or has expired." });

        return Ok(new { message = "Email confirmed. You can now log in." });
    }

    [HttpPost("resend-confirmation")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendConfirmation()
    {
        var userId = User.FindFirstValue("sub");
        var user = userId is not null ? await userManager.FindByIdAsync(userId) : null;
        if (user is null) return Unauthorized();

        if (user.EmailConfirmed)
            return BadRequest(new { message = "Email is already confirmed." });

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var confirmationLink = $"{emailOptions.Value.AppBaseUrl}/confirm-email?userId={user.Id}&token={encodedToken}";

        await emailService.SendEmailConfirmationAsync(
            user.Email!,
            $"{user.FirstName} {user.LastName}".Trim(),
            confirmationLink);

        return Ok(new { message = "Confirmation email sent." });
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
