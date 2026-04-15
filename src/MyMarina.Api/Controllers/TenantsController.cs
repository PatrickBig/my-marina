using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Tenants;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("tenants")]
[Authorize(Roles = nameof(UserRole.PlatformOperator))]
public class TenantsController(
    ICommandHandler<CreateTenantCommand, CreateTenantResult> createHandler,
    ICommandHandler<UpdateTenantCommand> updateHandler,
    IQueryHandler<GetTenantsQuery, IReadOnlyList<TenantDto>> getTenantsHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TenantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tenants = await getTenantsHandler.HandleAsync(new GetTenantsQuery(), ct);
        return Ok(tenants);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateTenantResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateTenantCommand command, CancellationToken ct)
    {
        try
        {
            var result = await createHandler.HandleAsync(command, ct);
            return CreatedAtAction(nameof(GetAll), result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantRequest request, CancellationToken ct)
    {
        try
        {
            var command = new UpdateTenantCommand(id, request.Name, request.IsActive, request.SubscriptionTier);
            await updateHandler.HandleAsync(command, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

public sealed record UpdateTenantRequest(string Name, bool IsActive, SubscriptionTier SubscriptionTier);
