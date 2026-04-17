using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Slips;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Authorize(Roles = "TenantOwner,MarinaManager,MarinaStaff")]
public class SlipsController(
    ICommandHandler<CreateSlipCommand, Guid> createHandler,
    ICommandHandler<UpdateSlipCommand> updateHandler,
    ICommandHandler<DeleteSlipCommand> deleteHandler,
    IQueryHandler<GetSlipsQuery, IReadOnlyList<SlipDto>> getSlipsHandler,
    IQueryHandler<GetAvailableSlipsQuery, IReadOnlyList<SlipDto>> availableHandler) : ControllerBase
{
    [HttpGet("marinas/{marinaId:guid}/slips")]
    [ProducesResponseType(typeof(IReadOnlyList<SlipDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid marinaId, CancellationToken ct)
    {
        var slips = await getSlipsHandler.HandleAsync(new GetSlipsQuery(marinaId), ct);
        return Ok(slips);
    }

    [HttpGet("marinas/{marinaId:guid}/slips/available")]
    [ProducesResponseType(typeof(IReadOnlyList<SlipDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailable(
        Guid marinaId,
        [FromQuery] decimal boatLength,
        [FromQuery] decimal boatBeam,
        [FromQuery] decimal boatDraft,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly? endDate,
        CancellationToken ct)
    {
        var query = new GetAvailableSlipsQuery(marinaId, boatLength, boatBeam, boatDraft, startDate, endDate);
        var slips = await availableHandler.HandleAsync(query, ct);
        return Ok(slips);
    }

    [HttpPost("marinas/{marinaId:guid}/slips")]
    [Authorize(Roles = "TenantOwner,MarinaManager")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid marinaId, [FromBody] CreateSlipRequest request, CancellationToken ct)
    {
        var command = new CreateSlipCommand(
            marinaId, request.DockId, request.Name, request.SlipType,
            request.MaxLength, request.MaxBeam, request.MaxDraft,
            request.HasElectric, request.Electric, request.HasWater,
            request.RateType, request.DailyRate, request.MonthlyRate, request.AnnualRate,
            request.Status, request.Latitude, request.Longitude, request.Notes);
        var id = await createHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetAll), new { marinaId }, id);
    }

    [HttpPut("slips/{id:guid}")]
    [Authorize(Roles = "TenantOwner,MarinaManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSlipRequest request, CancellationToken ct)
    {
        try
        {
            var command = new UpdateSlipCommand(
                id, request.Name, request.SlipType,
                request.MaxLength, request.MaxBeam, request.MaxDraft,
                request.HasElectric, request.Electric, request.HasWater,
                request.RateType, request.DailyRate, request.MonthlyRate, request.AnnualRate,
                request.Status, request.Latitude, request.Longitude, request.Notes);
            await updateHandler.HandleAsync(command, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("slips/{id:guid}")]
    [Authorize(Roles = "TenantOwner,MarinaManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await deleteHandler.HandleAsync(new DeleteSlipCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

public sealed record CreateSlipRequest(
    Guid? DockId,
    string Name,
    SlipType SlipType,
    decimal MaxLength,
    decimal MaxBeam,
    decimal MaxDraft,
    bool HasElectric,
    ElectricService? Electric,
    bool HasWater,
    RateType RateType,
    decimal? DailyRate,
    decimal? MonthlyRate,
    decimal? AnnualRate,
    SlipStatus Status,
    decimal? Latitude,
    decimal? Longitude,
    string? Notes);

public sealed record UpdateSlipRequest(
    string Name,
    SlipType SlipType,
    decimal MaxLength,
    decimal MaxBeam,
    decimal MaxDraft,
    bool HasElectric,
    ElectricService? Electric,
    bool HasWater,
    RateType RateType,
    decimal? DailyRate,
    decimal? MonthlyRate,
    decimal? AnnualRate,
    SlipStatus Status,
    decimal? Latitude,
    decimal? Longitude,
    string? Notes);
