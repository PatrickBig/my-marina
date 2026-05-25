using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.SlipAssignments;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Authorize]
public class SlipAssignmentsController(
    ICommandHandler<CreateSlipAssignmentCommand, SlipAssignmentDto> create,
    ICommandHandler<UpdateSlipAssignmentCommand, SlipAssignmentDto> update,
    ICommandHandler<EndSlipAssignmentCommand, SlipAssignmentDto> end,
    ICommandHandler<RenewSlipAssignmentCommand, SlipAssignmentDto> renew,
    IQueryHandler<GetSlipAssignmentsQuery, IReadOnlyList<SlipAssignmentDto>> list,
    IQueryHandler<GetSlipAssignmentQuery, SlipAssignmentDto> get,
    IQueryHandler<CheckSlipAvailabilityQuery, SlipAvailabilityResult> checkAvailability,
    IUserContext userContext)
    : ControllerBase
{
    // GET /marinas/{marinaId}/slip-assignments
    [HttpGet("marinas/{marinaId:guid}/slip-assignments")]
    [ProducesResponseType(typeof(IReadOnlyList<SlipAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        Guid marinaId,
        [FromQuery] Guid? slipId,
        [FromQuery] Guid? billingAccountId,
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        var items = await list.HandleAsync(new GetSlipAssignmentsQuery(
            marinaId, userContext.UserId!.Value, slipId, billingAccountId, activeOnly), ct);
        return Ok(items);
    }

    // GET /marinas/{marinaId}/slip-assignments/{id}
    [HttpGet("marinas/{marinaId:guid}/slip-assignments/{id:guid}")]
    [ProducesResponseType(typeof(SlipAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid marinaId, Guid id, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        try
        {
            var item = await get.HandleAsync(new GetSlipAssignmentQuery(id, marinaId, userContext.UserId!.Value), ct);
            return Ok(item);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // POST /marinas/{marinaId}/slip-assignments
    [HttpPost("marinas/{marinaId:guid}/slip-assignments")]
    [ProducesResponseType(typeof(SlipAssignmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(Guid marinaId, [FromBody] CreateSlipAssignmentRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();

        if (!Enum.TryParse<AssignmentType>(request.AssignmentType, ignoreCase: true, out var assignmentType))
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["assignmentType"] = [$"Unknown assignment type: {request.AssignmentType}"] }));

        try
        {
            var item = await create.HandleAsync(new CreateSlipAssignmentCommand(
                MarinaId:                marinaId,
                RequestingUserId:        userContext.UserId!.Value,
                SlipId:                  request.SlipId,
                BillingAccountId:        request.BillingAccountId,
                VesselId:                request.VesselId,
                AssignmentType:          assignmentType,
                StartDate:               request.StartDate,
                EndDate:                 request.EndDate,
                BaseRate:                request.BaseRate,
                AllowOwnerSubletWhenAway: request.AllowOwnerSubletWhenAway,
                AllowHolderSublet:       request.AllowHolderSublet,
                OwnerSubletShareToHolder: request.OwnerSubletShareToHolder,
                HolderSubletShareToOwner: request.HolderSubletShareToOwner,
                Notes:                   request.Notes), ct);

            return CreatedAtAction(nameof(Get), new { marinaId, id = item.Id }, item);
        }
        catch (KeyNotFoundException ex) { return NotFound(new ProblemDetails { Detail = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new ProblemDetails { Detail = ex.Message }); }
    }

    // PATCH /marinas/{marinaId}/slip-assignments/{id}
    [HttpPatch("marinas/{marinaId:guid}/slip-assignments/{id:guid}")]
    [ProducesResponseType(typeof(SlipAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid marinaId, Guid id, [FromBody] UpdateSlipAssignmentRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();

        AssignmentType? assignmentType = null;
        if (request.AssignmentType is not null)
        {
            if (!Enum.TryParse<AssignmentType>(request.AssignmentType, ignoreCase: true, out var parsed))
                return ValidationProblem(new ValidationProblemDetails(
                    new Dictionary<string, string[]> { ["assignmentType"] = [$"Unknown assignment type: {request.AssignmentType}"] }));
            assignmentType = parsed;
        }

        try
        {
            var item = await update.HandleAsync(new UpdateSlipAssignmentCommand(
                Id:                      id,
                MarinaId:                marinaId,
                RequestingUserId:        userContext.UserId!.Value,
                AssignmentType:          assignmentType,
                StartDate:               request.StartDate,
                EndDate:                 request.EndDate,
                BaseRate:                request.BaseRate,
                AllowOwnerSubletWhenAway: request.AllowOwnerSubletWhenAway,
                AllowHolderSublet:       request.AllowHolderSublet,
                OwnerSubletShareToHolder: request.OwnerSubletShareToHolder,
                HolderSubletShareToOwner: request.HolderSubletShareToOwner,
                Notes:                   request.Notes), ct);
            return Ok(item);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // POST /marinas/{marinaId}/slip-assignments/{id}/end
    [HttpPost("marinas/{marinaId:guid}/slip-assignments/{id:guid}/end")]
    [ProducesResponseType(typeof(SlipAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> End(Guid marinaId, Guid id, [FromBody] EndSlipAssignmentRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        try
        {
            var endDate = request.EndDate ?? DateOnly.FromDateTime(DateTime.Today);
            var item = await end.HandleAsync(new EndSlipAssignmentCommand(id, marinaId, userContext.UserId!.Value, endDate), ct);
            return Ok(item);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new ProblemDetails { Detail = ex.Message }); }
    }

    // POST /marinas/{marinaId}/slip-assignments/{id}/renew
    [HttpPost("marinas/{marinaId:guid}/slip-assignments/{id:guid}/renew")]
    [ProducesResponseType(typeof(SlipAssignmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Renew(Guid marinaId, Guid id, [FromBody] RenewSlipAssignmentRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        try
        {
            var item = await renew.HandleAsync(new RenewSlipAssignmentCommand(
                ExistingAssignmentId: id,
                MarinaId:             marinaId,
                RequestingUserId:     userContext.UserId!.Value,
                NewStartDate:         request.NewStartDate,
                NewEndDate:           request.NewEndDate), ct);
            return CreatedAtAction(nameof(Get), new { marinaId, id = item.Id }, item);
        }
        catch (KeyNotFoundException ex) { return NotFound(new ProblemDetails { Detail = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new ProblemDetails { Detail = ex.Message }); }
    }

    // GET /marinas/{marinaId}/slips/{slipId}/availability
    [HttpGet("marinas/{marinaId:guid}/slips/{slipId:guid}/availability")]
    [ProducesResponseType(typeof(SlipAvailabilityResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckAvailability(
        Guid marinaId, Guid slipId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] decimal? vesselLength,
        [FromQuery] decimal? vesselBeam,
        [FromQuery] decimal? vesselDraft,
        [FromQuery] Guid? excludeAssignmentId,
        CancellationToken ct = default)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        try
        {
            var result = await checkAvailability.HandleAsync(new CheckSlipAvailabilityQuery(
                SlipId:               slipId,
                MarinaId:             marinaId,
                StartDate:            startDate,
                EndDate:              endDate,
                VesselLength:         vesselLength,
                VesselBeam:           vesselBeam,
                VesselDraft:          vesselDraft,
                ExcludeAssignmentId:  excludeAssignmentId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}

// ---------- request records ----------

public sealed record CreateSlipAssignmentRequest(
    Guid SlipId,
    Guid BillingAccountId,
    Guid VesselId,
    string AssignmentType,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal BaseRate,
    bool AllowOwnerSubletWhenAway,
    bool AllowHolderSublet,
    decimal OwnerSubletShareToHolder,
    decimal HolderSubletShareToOwner,
    string? Notes
);

public sealed record UpdateSlipAssignmentRequest(
    string? AssignmentType,
    DateOnly? StartDate,
    DateOnly? EndDate,
    decimal? BaseRate,
    bool? AllowOwnerSubletWhenAway,
    bool? AllowHolderSublet,
    decimal? OwnerSubletShareToHolder,
    decimal? HolderSubletShareToOwner,
    string? Notes
);

public sealed record EndSlipAssignmentRequest(DateOnly? EndDate);

public sealed record RenewSlipAssignmentRequest(DateOnly NewStartDate, DateOnly? NewEndDate);
