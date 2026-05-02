using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Search;

namespace MyMarina.Api.Controllers;

[ApiController]
public class SlipSearchController(
    IQueryHandler<SearchSlipsQuery, IReadOnlyList<SlipSearchResultDto>> search,
    IQueryHandler<GetPublicSlipDetailQuery, SlipDetailDto> detail,
    IUserContext userContext)
    : ControllerBase
{
    // GET /slips/search — public, unauthenticated
    [HttpGet("slips/search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<SlipSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] decimal lat,
        [FromQuery] decimal lon,
        [FromQuery] decimal radiusMiles = 25,
        [FromQuery] DateOnly? arrivesAt = null,
        [FromQuery] DateOnly? departsAt = null,
        [FromQuery] decimal? vesselLength = null,
        [FromQuery] decimal? vesselBeam = null,
        [FromQuery] decimal? vesselDraft = null,
        [FromQuery] string? slipType = null,
        [FromQuery] bool? hasElectric = null,
        [FromQuery] bool? hasWater = null,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var arrives = arrivesAt ?? DateOnly.FromDateTime(DateTime.Today);
        var departs = departsAt ?? arrives.AddDays(1);

        if (departs <= arrives)
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["departsAt"] = ["Departure must be after arrival."] }));

        var clampedPageSize = Math.Clamp(pageSize, 1, 50);
        var clampedRadius   = Math.Clamp(radiusMiles, 1, 100);

        var results = await search.HandleAsync(new SearchSlipsQuery(
            Latitude:      lat,
            Longitude:     lon,
            RadiusMiles:   clampedRadius,
            ArrivesAt:     arrives,
            DepartsAt:     departs,
            VesselLength:  vesselLength,
            VesselBeam:    vesselBeam,
            VesselDraft:   vesselDraft,
            SlipType:      slipType,
            HasElectric:   hasElectric,
            HasWater:      hasWater,
            Page:          page,
            PageSize:      clampedPageSize,
            IncludeDemo:   userContext.IsDemo), ct);

        return Ok(results);
    }

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
