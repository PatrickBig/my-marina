using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Vessels;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("vessels")]
[Authorize]
public class VesselsController(
    ICommandHandler<CreateVesselCommand, VesselDto> createVessel,
    ICommandHandler<UpdateVesselCommand> updateVessel,
    ICommandHandler<ArchiveVesselCommand> archiveVessel,
    IQueryHandler<GetMyVesselsQuery, IReadOnlyList<VesselDto>> getMyVessels,
    IQueryHandler<GetVesselQuery, VesselDto> getVessel,
    IQueryHandler<GetPendingVesselClaimsQuery, IReadOnlyList<PendingVesselClaimDto>> getPendingClaims,
    ICommandHandler<ClaimVesselCommand, VesselDto> claimVessel,
    ICommandHandler<RejectVesselClaimCommand> rejectClaim,
    IUserContext userContext)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VesselDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyVessels(CancellationToken ct)
    {
        var vessels = await getMyVessels.HandleAsync(new GetMyVesselsQuery(userContext.UserId!.Value), ct);
        return Ok(vessels);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VesselDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVessel(Guid id, CancellationToken ct)
    {
        try
        {
            var vessel = await getVessel.HandleAsync(new GetVesselQuery(id, userContext.UserId!.Value), ct);
            return Ok(vessel);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(VesselDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateVessel([FromBody] CreateVesselRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<BoatType>(request.BoatType, ignoreCase: true, out var boatType))
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["boatType"] = [$"Unknown boat type: {request.BoatType}"] }));

        var vessel = await createVessel.HandleAsync(new CreateVesselCommand(
            OwnerId:            userContext.UserId!.Value,
            Name:               request.Name,
            Make:               request.Make,
            Model:              request.Model,
            Year:               request.Year,
            Length:             request.Length,
            Beam:               request.Beam,
            Draft:              request.Draft,
            BoatType:           boatType,
            HullColor:          request.HullColor,
            RegistrationNumber: request.RegistrationNumber,
            RegistrationState:  request.RegistrationState), ct);

        return CreatedAtAction(nameof(GetVessel), new { id = vessel.Id }, vessel);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVessel(Guid id, [FromBody] UpdateVesselRequest request, CancellationToken ct)
    {
        BoatType? boatType = null;
        if (request.BoatType is not null)
        {
            if (!Enum.TryParse<BoatType>(request.BoatType, ignoreCase: true, out var parsed))
                return ValidationProblem(new ValidationProblemDetails(
                    new Dictionary<string, string[]> { ["boatType"] = [$"Unknown boat type: {request.BoatType}"] }));
            boatType = parsed;
        }

        try
        {
            await updateVessel.HandleAsync(new UpdateVesselCommand(
                Id:                 id,
                OwnerId:            userContext.UserId!.Value,
                Name:               request.Name,
                Make:               request.Make,
                Model:              request.Model,
                Year:               request.Year,
                Length:             request.Length,
                Beam:               request.Beam,
                Draft:              request.Draft,
                BoatType:           boatType,
                HullColor:          request.HullColor,
                RegistrationNumber: request.RegistrationNumber,
                RegistrationState:  request.RegistrationState), ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveVessel(Guid id, CancellationToken ct)
    {
        try
        {
            await archiveVessel.HandleAsync(new ArchiveVesselCommand(id, userContext.UserId!.Value), ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // ---------- Ghost vessel claim flow ----------

    [HttpGet("pending-claims")]
    [ProducesResponseType(typeof(IReadOnlyList<PendingVesselClaimDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingClaims(CancellationToken ct)
    {
        var claims = await getPendingClaims.HandleAsync(
            new GetPendingVesselClaimsQuery(userContext.UserId!.Value, userContext.Email!), ct);
        return Ok(claims);
    }

    [HttpPost("{id:guid}/claim")]
    [ProducesResponseType(typeof(VesselDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ClaimVessel(Guid id, CancellationToken ct)
    {
        try
        {
            var vessel = await claimVessel.HandleAsync(
                new ClaimVesselCommand(id, userContext.UserId!.Value, userContext.Email!), ct);
            return Ok(vessel);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new ProblemDetails { Detail = ex.Message }); }
    }

    [HttpPost("{id:guid}/reject-claim")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RejectClaim(Guid id, CancellationToken ct)
    {
        try
        {
            await rejectClaim.HandleAsync(
                new RejectVesselClaimCommand(id, userContext.UserId!.Value, userContext.Email!), ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new ProblemDetails { Detail = ex.Message }); }
    }
}

// ---------- request records ----------

public sealed record CreateVesselRequest(
    string Name,
    string? Make,
    string? Model,
    int? Year,
    decimal Length,
    decimal Beam,
    decimal Draft,
    string BoatType,
    string? HullColor,
    string? RegistrationNumber,
    string? RegistrationState
);

public sealed record UpdateVesselRequest(
    string? Name,
    string? Make,
    string? Model,
    int? Year,
    decimal? Length,
    decimal? Beam,
    decimal? Draft,
    string? BoatType,
    string? HullColor,
    string? RegistrationNumber,
    string? RegistrationState
);
