using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Search;

namespace MyMarina.Api.Controllers;

[ApiController]
public class MarinaSearchController(
    IQueryHandler<MarinaRollupSearchQuery, IReadOnlyList<MarinaRollupResultDto>> rollupSearch,
    IQueryHandler<SearchSlipsAtMarinaQuery, IReadOnlyList<SlipSearchResultDto>> slipsAtMarina,
    IUserContext userContext)
    : ControllerBase
{
    // GET /marinas/search — public, bounding-box marina rollup
    [HttpGet("marinas/search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<MarinaRollupResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchMarinas(
        [FromQuery] decimal north,
        [FromQuery] decimal south,
        [FromQuery] decimal east,
        [FromQuery] decimal west,
        [FromQuery] string? listingKind = null,
        [FromQuery] DateOnly? arrivesAt = null,
        [FromQuery] DateOnly? departsAt = null,
        [FromQuery] string? leaseTerm = null,
        [FromQuery] decimal? vesselLength = null,
        [FromQuery] decimal? vesselBeam = null,
        [FromQuery] decimal? vesselDraft = null,
        [FromQuery] string? slipType = null,
        [FromQuery] bool? hasElectric = null,
        [FromQuery] bool? hasWater = null,
        [FromQuery] decimal? priceMin = null,
        [FromQuery] decimal? priceMax = null,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (north <= south || east <= west)
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["bounds"] = ["north must exceed south and east must exceed west."]
                }));

        if ((priceMin.HasValue || priceMax.HasValue) && listingKind is null)
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["priceMin"] = ["priceMin and priceMax require listingKind to be specified."]
                }));

        string effectiveListingKind = listingKind ?? "Transient";
        bool isLease = string.Equals(effectiveListingKind, "Lease", StringComparison.OrdinalIgnoreCase);

        DateOnly? arrives = arrivesAt;
        DateOnly? departs = departsAt;
        if (!isLease)
        {
            arrives ??= DateOnly.FromDateTime(DateTime.Today);
            departs ??= arrives.Value.AddDays(1);
            if (departs <= arrives)
                return ValidationProblem(new ValidationProblemDetails(
                    new Dictionary<string, string[]> { ["departsAt"] = ["Departure must be after arrival."] }));
        }

        var results = await rollupSearch.HandleAsync(new MarinaRollupSearchQuery(
            North:        north,
            South:        south,
            East:         east,
            West:         west,
            ArrivesAt:    arrives,
            DepartsAt:    departs,
            VesselLength: vesselLength,
            VesselBeam:   vesselBeam,
            VesselDraft:  vesselDraft,
            SlipType:     slipType,
            HasElectric:  hasElectric,
            HasWater:     hasWater,
            ListingKind:  effectiveListingKind,
            LeaseTerm:    leaseTerm,
            PriceMin:     priceMin,
            PriceMax:     priceMax,
            Page:         page,
            PageSize:     Math.Clamp(pageSize, 1, 50),
            IncludeDemo:  userContext.IsDemo), ct);

        return Ok(results);
    }

    // GET /marinas/{id}/slips/search — public, per-marina slip list
    [HttpGet("marinas/{id:guid}/slips/search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<SlipSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SearchSlipsAtMarina(
        Guid id,
        [FromQuery] string listingKind = "Transient",
        [FromQuery] DateOnly? arrivesAt = null,
        [FromQuery] DateOnly? departsAt = null,
        [FromQuery] string? leaseTerm = null,
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
        bool isLease = string.Equals(listingKind, "Lease", StringComparison.OrdinalIgnoreCase);

        DateOnly? arrives = arrivesAt;
        DateOnly? departs = departsAt;
        if (!isLease)
        {
            arrives ??= DateOnly.FromDateTime(DateTime.Today);
            departs ??= arrives.Value.AddDays(1);
            if (departs <= arrives)
                return ValidationProblem(new ValidationProblemDetails(
                    new Dictionary<string, string[]> { ["departsAt"] = ["Departure must be after arrival."] }));
        }

        try
        {
            var results = await slipsAtMarina.HandleAsync(new SearchSlipsAtMarinaQuery(
                MarinaId:     id,
                ArrivesAt:    arrives,
                DepartsAt:    departs,
                VesselLength: vesselLength,
                VesselBeam:   vesselBeam,
                VesselDraft:  vesselDraft,
                SlipType:     slipType,
                HasElectric:  hasElectric,
                HasWater:     hasWater,
                ListingKind:  listingKind,
                LeaseTerm:    leaseTerm,
                Page:         page,
                PageSize:     Math.Clamp(pageSize, 1, 50),
                IncludeDemo:  userContext.IsDemo), ct);

            return Ok(results);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
