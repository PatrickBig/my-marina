using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.AvailabilityWindows;
using MyMarina.Application.Sublet;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Authorize]
public class SubletController(
    IQueryHandler<GetMySlipAssignmentsQuery, IReadOnlyList<MySlipAssignmentDto>> myAssignments,
    ICommandHandler<CreateOwnerAbsenceCommand, OwnerAbsenceDto> createAbsence,
    ICommandHandler<DeleteOwnerAbsenceCommand, bool> deleteAbsence,
    IQueryHandler<GetOwnerAbsencesQuery, IReadOnlyList<OwnerAbsenceDto>> getAbsences,
    IQueryHandler<GetMarinaAbsencesQuery, IReadOnlyList<OwnerAbsenceDto>> marinaAbsences,
    ICommandHandler<CreateHolderSubletWindowCommand, AvailabilityWindowDto> createHolderWindow,
    IUserContext userContext)
    : ControllerBase
{
    // GET /me/slip-assignments — boater's own slip assignments (via billing account memberships)
    [HttpGet("me/slip-assignments")]
    [ProducesResponseType(typeof(IReadOnlyList<MySlipAssignmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyAssignments(CancellationToken ct)
    {
        var billingAccountIds = userContext.BillingAccounts.Select(b => b.BillingAccountId).ToList();
        var items = await myAssignments.HandleAsync(
            new GetMySlipAssignmentsQuery(userContext.UserId!.Value, billingAccountIds), ct);
        return Ok(items);
    }

    // POST /slip-assignments/{id}/away — boater marks themselves away
    [HttpPost("slip-assignments/{id:guid}/away")]
    [ProducesResponseType(typeof(OwnerAbsenceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkAway(Guid id, [FromBody] MarkAwayRequest request, CancellationToken ct)
    {
        var billingAccountIds = userContext.BillingAccounts.Select(b => b.BillingAccountId).ToList();
        if (billingAccountIds.Count == 0) return Forbid();
        try
        {
            var dto = await createAbsence.HandleAsync(new CreateOwnerAbsenceCommand(
                SlipAssignmentId:               id,
                RequestingUserId:               userContext.UserId!.Value,
                RequestingUserBillingAccountIds: billingAccountIds,
                StartsOn:                       request.StartsOn,
                EndsOn:                         request.EndsOn,
                Notes:                          request.Notes), ct);
            return CreatedAtAction(nameof(GetAbsences), new { id }, dto);
        }
        catch (KeyNotFoundException ex) { return NotFound(new ProblemDetails { Detail = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new ProblemDetails { Detail = ex.Message }); }
    }

    // GET /slip-assignments/{id}/absences — boater lists their absences
    [HttpGet("slip-assignments/{id:guid}/absences")]
    [ProducesResponseType(typeof(IReadOnlyList<OwnerAbsenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAbsences(Guid id, CancellationToken ct)
    {
        var billingAccountIds = userContext.BillingAccounts.Select(b => b.BillingAccountId).ToList();
        if (billingAccountIds.Count == 0) return Forbid();
        try
        {
            var items = await getAbsences.HandleAsync(new GetOwnerAbsencesQuery(
                id, userContext.UserId!.Value, billingAccountIds), ct);
            return Ok(items);
        }
        catch (KeyNotFoundException ex) { return NotFound(new ProblemDetails { Detail = ex.Message }); }
    }

    // DELETE /slip-assignments/{id}/absences/{absenceId} — boater removes an absence
    [HttpDelete("slip-assignments/{id:guid}/absences/{absenceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAbsence(Guid id, Guid absenceId, CancellationToken ct)
    {
        var billingAccountIds = userContext.BillingAccounts.Select(b => b.BillingAccountId).ToList();
        if (billingAccountIds.Count == 0) return Forbid();
        try
        {
            await deleteAbsence.HandleAsync(new DeleteOwnerAbsenceCommand(
                absenceId, id, userContext.UserId!.Value, billingAccountIds), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new ProblemDetails { Detail = ex.Message }); }
    }

    // POST /slip-assignments/{id}/sublet-window — holder creates their own sublet listing
    [HttpPost("slip-assignments/{id:guid}/sublet-window")]
    [ProducesResponseType(typeof(AvailabilityWindowDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSubletWindow(
        Guid id, [FromBody] CreateSubletWindowRequest request, CancellationToken ct)
    {
        var billingAccountIds = userContext.BillingAccounts.Select(b => b.BillingAccountId).ToList();
        if (billingAccountIds.Count == 0) return Forbid();
        try
        {
            var dto = await createHolderWindow.HandleAsync(new CreateHolderSubletWindowCommand(
                SlipAssignmentId:               id,
                RequestingUserId:               userContext.UserId!.Value,
                RequestingUserBillingAccountIds: billingAccountIds,
                StartsAt:                       request.StartsAt,
                EndsAt:                         request.EndsAt,
                InstantBook:                    request.InstantBook,
                MinNights:                      request.MinNights,
                MaxNights:                      request.MaxNights,
                BasePricePerNight:              request.BasePricePerNight,
                WeeklyDiscount:                 request.WeeklyDiscount,
                MonthlyDiscount:                request.MonthlyDiscount,
                CleaningFee:                    request.CleaningFee), ct);
            return StatusCode(StatusCodes.Status201Created, dto);
        }
        catch (KeyNotFoundException ex) { return NotFound(new ProblemDetails { Detail = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new ProblemDetails { Detail = ex.Message }); }
    }

    // GET /marinas/{marinaId}/absences — marina staff sees all reported absences at their marina
    [HttpGet("marinas/{marinaId:guid}/absences")]
    [ProducesResponseType(typeof(IReadOnlyList<OwnerAbsenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarinaAbsences(
        Guid marinaId, [FromQuery] Guid? slipId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        var items = await marinaAbsences.HandleAsync(
            new GetMarinaAbsencesQuery(marinaId, userContext.UserId!.Value, slipId), ct);
        return Ok(items);
    }
}

// ---------- request records ----------

public sealed record MarkAwayRequest(
    DateOnly StartsOn,
    DateOnly EndsOn,
    string? Notes
);

public sealed record CreateSubletWindowRequest(
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
