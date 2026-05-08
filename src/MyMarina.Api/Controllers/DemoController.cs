using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Demo;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("demo")]
public class DemoController(IQueryHandler<DemoSessionQuery, DemoSessionResponse> sessionQuery)
    : ControllerBase
{
    /// <summary>
    /// Issues a short-lived (30 min), read-only demo JWT. No account required.
    /// Called by the marketing site's "Try the demo" button.
    /// </summary>
    [HttpPost("session")]
    [ProducesResponseType(typeof(DemoSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateSession(CancellationToken ct)
    {
        try
        {
            var result = await sessionQuery.HandleAsync(new DemoSessionQuery(), ct);
            return Ok(new DemoSessionDto(result.AccessToken, result.ExpiresAt, IsDemo: true));
        }
        catch (InvalidOperationException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}

public sealed record DemoSessionDto(string AccessToken, string ExpiresAt, bool IsDemo);
