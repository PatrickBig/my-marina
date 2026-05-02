using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;
using MyMarina.Infrastructure.Identity;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    ICommandHandler<RegisterCommand> register,
    ICommandHandler<LoginCommand, AuthResponse> login,
    ICommandHandler<RefreshTokenCommand, AuthResponse> refresh,
    ICommandHandler<LogoutCommand> logout,
    ICommandHandler<ForgotPasswordCommand> forgotPassword,
    ICommandHandler<ResetPasswordCommand> resetPassword,
    ICommandHandler<ConfirmEmailCommand> confirmEmail,
    ICommandHandler<ResendConfirmationCommand> resendConfirmation)
    : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var command = new RegisterCommand(
            request.Email, request.Password, request.FirstName, request.LastName,
            request.MarketingOptIn, request.TermsAccepted);
        try
        {
            await register.HandleAsync(command, ct);
            return NoContent();
        }
        catch (IdentityException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(
                ex.Errors.GroupBy(e => e.Code).ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray())));
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        try
        {
            var response = await login.HandleAsync(new LoginCommand(request.Email, request.Password, ip), ct);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        try
        {
            var response = await refresh.HandleAsync(new RefreshTokenCommand(request.RefreshToken, ip), ct);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        await logout.HandleAsync(new LogoutCommand(request.RefreshToken), ct);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await forgotPassword.HandleAsync(new ForgotPasswordCommand(request.Email), ct);
        return NoContent();
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        try
        {
            await resetPassword.HandleAsync(new ResetPasswordCommand(request.Email, request.Token, request.NewPassword), ct);
            return NoContent();
        }
        catch (IdentityException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(
                ex.Errors.GroupBy(e => e.Code).ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray())));
        }
    }

    [HttpPost("confirm-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken ct)
    {
        try
        {
            await confirmEmail.HandleAsync(new ConfirmEmailCommand(request.UserId, request.Token), ct);
            return NoContent();
        }
        catch (IdentityException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(
                ex.Errors.GroupBy(e => e.Code).ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray())));
        }
    }

    [HttpPost("resend-confirmation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request, CancellationToken ct)
    {
        await resendConfirmation.HandleAsync(new ResendConfirmationCommand(request.Email), ct);
        return NoContent();
    }
}

// ---------- request records ----------

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    bool MarketingOptIn,
    bool TermsAccepted
);

public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
public sealed record ConfirmEmailRequest(string UserId, string Token);
public sealed record ResendConfirmationRequest(string Email);
