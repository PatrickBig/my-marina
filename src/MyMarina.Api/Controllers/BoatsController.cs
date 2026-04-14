using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Boats;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{nameof(UserRole.MarinaOwner)},{nameof(UserRole.MarinaStaff)}")]
public class BoatsController(
    ICommandHandler<CreateBoatCommand, Guid> createHandler,
    ICommandHandler<UpdateBoatCommand> updateHandler,
    ICommandHandler<DeleteBoatCommand> deleteHandler,
    IQueryHandler<GetBoatsQuery, IReadOnlyList<BoatDto>> getBoatsHandler) : ControllerBase
{
    [HttpGet("customers/{customerAccountId:guid}/boats")]
    [ProducesResponseType(typeof(IReadOnlyList<BoatDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid customerAccountId, CancellationToken ct)
    {
        var boats = await getBoatsHandler.HandleAsync(new GetBoatsQuery(customerAccountId), ct);
        return Ok(boats);
    }

    [HttpPost("customers/{customerAccountId:guid}/boats")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid customerAccountId, [FromBody] CreateBoatRequest request, CancellationToken ct)
    {
        var command = new CreateBoatCommand(
            customerAccountId, request.Name, request.Make, request.Model, request.Year,
            request.Length, request.Beam, request.Draft, request.BoatType,
            request.HullColor, request.RegistrationNumber, request.RegistrationState,
            request.InsuranceProvider, request.InsurancePolicyNumber, request.InsuranceExpiresOn);
        var id = await createHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetAll), new { customerAccountId }, id);
    }

    [HttpPut("boats/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBoatRequest request, CancellationToken ct)
    {
        try
        {
            var command = new UpdateBoatCommand(
                id, request.Name, request.Make, request.Model, request.Year,
                request.Length, request.Beam, request.Draft, request.BoatType,
                request.HullColor, request.RegistrationNumber, request.RegistrationState,
                request.InsuranceProvider, request.InsurancePolicyNumber, request.InsuranceExpiresOn);
            await updateHandler.HandleAsync(command, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("boats/{id:guid}")]
    [Authorize(Roles = nameof(UserRole.MarinaOwner))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await deleteHandler.HandleAsync(new DeleteBoatCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

public sealed record CreateBoatRequest(
    string Name,
    string? Make,
    string? Model,
    int? Year,
    decimal Length,
    decimal Beam,
    decimal Draft,
    BoatType BoatType,
    string? HullColor,
    string? RegistrationNumber,
    string? RegistrationState,
    string? InsuranceProvider,
    string? InsurancePolicyNumber,
    DateOnly? InsuranceExpiresOn);

public sealed record UpdateBoatRequest(
    string Name,
    string? Make,
    string? Model,
    int? Year,
    decimal Length,
    decimal Beam,
    decimal Draft,
    BoatType BoatType,
    string? HullColor,
    string? RegistrationNumber,
    string? RegistrationState,
    string? InsuranceProvider,
    string? InsurancePolicyNumber,
    DateOnly? InsuranceExpiresOn);
