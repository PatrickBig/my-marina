using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Search;

namespace MyMarina.Api.Controllers;

[ApiController]
public class SlipDetailController(
    IQueryHandler<GetPublicSlipDetailQuery, SlipDetailDto> detail,
    IUserContext userContext)
    : ControllerBase
{
    // GET /slips/{id} — public, unauthenticated
    [HttpGet("slips/{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SlipDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct)
    {
        try
        {
            var dto = await detail.HandleAsync(new GetPublicSlipDetailQuery(id, userContext.IsDemo), ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
