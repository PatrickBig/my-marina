using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.AvailabilityWindows;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Authorize]
public class AvailabilityWindowsController(
    ICommandHandler<CreateAvailabilityWindowCommand, AvailabilityWindowDto> create,
    ICommandHandler<UpdateAvailabilityWindowCommand, AvailabilityWindowDto> update,
    ICommandHandler<SetAvailabilityWindowStatusCommand, AvailabilityWindowDto> setStatus,
    IQueryHandler<GetAvailabilityWindowsQuery, IReadOnlyList<AvailabilityWindowDto>> list,
    IQueryHandler<GetAvailabilityWindowQuery, AvailabilityWindowDto> get,
    IUserContext userContext)
    : ControllerBase
{
    // GET /marinas/{marinaId}/availability-windows
    [HttpGet("marinas/{marinaId:guid}/availability-windows")]
    [ProducesResponseType(typeof(IReadOnlyList<AvailabilityWindowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        Guid marinaId,
        [FromQuery] Guid? slipId,
        [FromQuery] string? status,
        CancellationToken ct = default)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        var items = await list.HandleAsync(new GetAvailabilityWindowsQuery(
            marinaId, userContext.UserId!.Value, slipId, status), ct);
        return Ok(items);
    }

    // GET /marinas/{marinaId}/availability-windows/{id}
    [HttpGet("marinas/{marinaId:guid}/availability-windows/{id:guid}")]
    [ProducesResponseType(typeof(AvailabilityWindowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid marinaId, Guid id, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        try
        {
            var item = await get.HandleAsync(new GetAvailabilityWindowQuery(id, marinaId, userContext.UserId!.Value), ct);
            return Ok(item);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // POST /marinas/{marinaId}/availability-windows
    [HttpPost("marinas/{marinaId:guid}/availability-windows")]
    [ProducesResponseType(typeof(AvailabilityWindowDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(Guid marinaId, [FromBody] CreateAvailabilityWindowRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();

        if (!Enum.TryParse<ListedByKind>(request.ListedByKind, ignoreCase: true, out var listedByKind))
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["listedByKind"] = [$"Unknown kind: {request.ListedByKind}"] }));

        try
        {
            var item = await create.HandleAsync(new CreateAvailabilityWindowCommand(
                MarinaId:                marinaId,
                RequestingUserId:        userContext.UserId!.Value,
                SlipId:                  request.SlipId,
                ListedByKind:            listedByKind,
                ListedByMarinaId:        request.ListedByMarinaId,
                ListedByBillingAccountId: request.ListedByBillingAccountId,
                RelatedAssignmentId:     request.RelatedAssignmentId,
                StartsAt:                request.StartsAt,
                EndsAt:                  request.EndsAt,
                InstantBook:             request.InstantBook,
                MinNights:               request.MinNights,
                MaxNights:               request.MaxNights,
                BasePricePerNight:       request.BasePricePerNight,
                WeeklyDiscount:          request.WeeklyDiscount,
                MonthlyDiscount:         request.MonthlyDiscount,
                CleaningFee:             request.CleaningFee), ct);

            return CreatedAtAction(nameof(Get), new { marinaId, id = item.Id }, item);
        }
        catch (KeyNotFoundException ex) { return NotFound(new ProblemDetails { Detail = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new ProblemDetails { Detail = ex.Message }); }
    }

    // PATCH /marinas/{marinaId}/availability-windows/{id}
    [HttpPatch("marinas/{marinaId:guid}/availability-windows/{id:guid}")]
    [ProducesResponseType(typeof(AvailabilityWindowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid marinaId, Guid id, [FromBody] UpdateAvailabilityWindowRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        try
        {
            var item = await update.HandleAsync(new UpdateAvailabilityWindowCommand(
                Id:               id,
                MarinaId:         marinaId,
                RequestingUserId: userContext.UserId!.Value,
                StartsAt:         request.StartsAt,
                EndsAt:           request.EndsAt,
                InstantBook:      request.InstantBook,
                MinNights:        request.MinNights,
                MaxNights:        request.MaxNights,
                BasePricePerNight: request.BasePricePerNight,
                WeeklyDiscount:   request.WeeklyDiscount,
                MonthlyDiscount:  request.MonthlyDiscount,
                CleaningFee:      request.CleaningFee), ct);
            return Ok(item);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new ProblemDetails { Detail = ex.Message }); }
    }

    // POST /marinas/{marinaId}/availability-windows/{id}/status
    [HttpPost("marinas/{marinaId:guid}/availability-windows/{id:guid}/status")]
    [ProducesResponseType(typeof(AvailabilityWindowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetStatus(Guid marinaId, Guid id, [FromBody] SetAvailabilityWindowStatusRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();

        if (!Enum.TryParse<AvailabilityWindowStatus>(request.Status, ignoreCase: true, out var status))
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["status"] = [$"Unknown status: {request.Status}"] }));

        try
        {
            var item = await setStatus.HandleAsync(
                new SetAvailabilityWindowStatusCommand(id, marinaId, userContext.UserId!.Value, status), ct);
            return Ok(item);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new ProblemDetails { Detail = ex.Message }); }
    }
}

// ---------- request records ----------

public sealed record CreateAvailabilityWindowRequest(
    Guid SlipId,
    string ListedByKind,
    Guid? ListedByMarinaId,
    Guid? ListedByBillingAccountId,
    Guid? RelatedAssignmentId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool InstantBook,
    int? MinNights,
    int? MaxNights,
    decimal BasePricePerNight,
    decimal? WeeklyDiscount,
    decimal? MonthlyDiscount,
    decimal? CleaningFee
);

public sealed record UpdateAvailabilityWindowRequest(
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    bool? InstantBook,
    int? MinNights,
    int? MaxNights,
    decimal? BasePricePerNight,
    decimal? WeeklyDiscount,
    decimal? MonthlyDiscount,
    decimal? CleaningFee
);

public sealed record SetAvailabilityWindowStatusRequest(string Status);
