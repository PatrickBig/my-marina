using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;
using MyMarina.Infrastructure.Identity;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("me")]
[Authorize]
public class MeController(
    IQueryHandler<MeQuery, MeResponse> me,
    ICommandHandler<UpdateProfileCommand> updateProfile)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var response = await me.HandleAsync(new MeQuery(), ct);
        return Ok(response);
    }

    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        try
        {
            await updateProfile.HandleAsync(new UpdateProfileCommand(
                request.FirstName, request.LastName, request.PhoneNumber, request.MarketingOptIn), ct);
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
}

public sealed record UpdateProfileRequest(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    bool? MarketingOptIn
);
