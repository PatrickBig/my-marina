using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Reservations;

namespace MyMarina.Api.Controllers;

[ApiController]
[Authorize]
public class ReservationsController(
    ICommandHandler<CreateReservationCommand, ReservationDto> create,
    ICommandHandler<ApproveReservationCommand, ReservationDto> approve,
    ICommandHandler<DeclineReservationCommand, ReservationDto> decline,
    ICommandHandler<CancelReservationCommand, ReservationDto> cancel,
    ICommandHandler<MarkNoShowCommand, ReservationDto> noShow,
    IQueryHandler<GetMyTripsQuery, IReadOnlyList<ReservationDto>> myTrips,
    IQueryHandler<GetMarinaReservationsQuery, IReadOnlyList<ReservationDto>> marinaReservations,
    IQueryHandler<GetReservationQuery, ReservationDto> get,
    IUserContext userContext)
    : ControllerBase
{
    // POST /reservations — boater creates a booking
    [HttpPost("reservations")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateReservationRequest request, CancellationToken ct)
    {
        try
        {
            var dto = await create.HandleAsync(new CreateReservationCommand(
                RequestingUserId:      userContext.UserId!.Value,
                VesselId:              request.VesselId,
                AvailabilityWindowId:  request.AvailabilityWindowId,
                SlipId:                request.SlipId,
                ArrivesAt:             request.ArrivesAt,
                DepartsAt:             request.DepartsAt,
                Notes:                 request.Notes), ct);

            return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
        }
        catch (KeyNotFoundException ex) { return NotFound(new ProblemDetails { Detail = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new ProblemDetails { Detail = ex.Message }); }
    }

    // GET /reservations/my-trips — boater's own reservations
    [HttpGet("reservations/my-trips")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyTrips([FromQuery] string? status, CancellationToken ct)
    {
        var items = await myTrips.HandleAsync(new GetMyTripsQuery(userContext.UserId!.Value, status), ct);
        return Ok(items);
    }

    // GET /reservations/{id} — single reservation (boater or marina staff)
    [HttpGet("reservations/{id:guid}")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        try
        {
            var dto = await get.HandleAsync(new GetReservationQuery(id, userContext.UserId!.Value), ct);

            // Allow boater who made it OR anyone with marina access at the slip's marina
            if (dto.BoaterUserId != userContext.UserId!.Value && !userContext.HasMarinaAccess(dto.MarinaId))
                return Forbid();

            return Ok(dto);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // GET /marinas/{marinaId}/reservations — host inbox
    [HttpGet("marinas/{marinaId:guid}/reservations")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarinaInbox(Guid marinaId, [FromQuery] string? status, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        var items = await marinaReservations.HandleAsync(
            new GetMarinaReservationsQuery(marinaId, userContext.UserId!.Value, status), ct);
        return Ok(items);
    }

    // POST /marinas/{marinaId}/reservations/{id}/approve
    [HttpPost("marinas/{marinaId:guid}/reservations/{id:guid}/approve")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(Guid marinaId, Guid id, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        try
        {
            var dto = await approve.HandleAsync(
                new ApproveReservationCommand(id, marinaId, userContext.UserId!.Value), ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new ProblemDetails { Detail = ex.Message }); }
    }

    // POST /marinas/{marinaId}/reservations/{id}/decline
    [HttpPost("marinas/{marinaId:guid}/reservations/{id:guid}/decline")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Decline(Guid marinaId, Guid id, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        try
        {
            var dto = await decline.HandleAsync(
                new DeclineReservationCommand(id, marinaId, userContext.UserId!.Value), ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new ProblemDetails { Detail = ex.Message }); }
    }

    // POST /reservations/{id}/cancel — boater or marina host
    [HttpPost("reservations/{id:guid}/cancel")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        try
        {
            // Pre-load to authorize — boater or marina staff
            var dto = await get.HandleAsync(new GetReservationQuery(id, userContext.UserId!.Value), ct);
            if (dto.BoaterUserId != userContext.UserId!.Value && !userContext.HasMarinaAccess(dto.MarinaId))
                return Forbid();

            var updated = await cancel.HandleAsync(
                new CancelReservationCommand(id, userContext.UserId!.Value), ct);
            return Ok(updated);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new ProblemDetails { Detail = ex.Message }); }
    }

    // POST /marinas/{marinaId}/reservations/{id}/no-show
    [HttpPost("marinas/{marinaId:guid}/reservations/{id:guid}/no-show")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> NoShow(Guid marinaId, Guid id, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        try
        {
            var dto = await noShow.HandleAsync(
                new MarkNoShowCommand(id, marinaId, userContext.UserId!.Value), ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new ProblemDetails { Detail = ex.Message }); }
    }
}

// ---------- request records ----------

public sealed record CreateReservationRequest(
    Guid VesselId,
    Guid? AvailabilityWindowId,
    Guid? SlipId,
    DateTimeOffset ArrivesAt,
    DateTimeOffset DepartsAt,
    string? Notes
);
