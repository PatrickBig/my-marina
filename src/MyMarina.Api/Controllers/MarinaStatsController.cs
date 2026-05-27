using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;

namespace MyMarina.Api.Controllers;

[ApiController]
[Authorize]
public class MarinaStatsController(
    IQueryHandler<GetMarinaCompositionQuery, MarinaCompositionDto> getComposition,
    IQueryHandler<GetBillingSummaryQuery, BillingSummaryDto> getBillingSummary,
    IUserContext userContext)
    : ControllerBase
{
    // GET /marinas/{marinaId}/composition
    [HttpGet("marinas/{marinaId:guid}/composition")]
    [ProducesResponseType(typeof(MarinaCompositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> GetComposition(Guid marinaId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId))
            return Forbid();

        var result = await getComposition.HandleAsync(new GetMarinaCompositionQuery(marinaId), ct);
        return Ok(result);
    }

    // GET /marinas/{marinaId}/billing-summary
    [HttpGet("marinas/{marinaId:guid}/billing-summary")]
    [ProducesResponseType(typeof(BillingSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> GetBillingSummary(Guid marinaId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId))
            return Forbid();

        var result = await getBillingSummary.HandleAsync(new GetBillingSummaryQuery(marinaId), ct);
        return Ok(result);
    }
}
