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
    IQueryHandler<GetTenantsQuery, IReadOnlyList<TenantDto>> getTenantsHandler,
    IQueryHandler<GetTenantQuery, TenantDetailDto?> getTenantHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TenantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tenants = await getTenantsHandler.HandleAsync(new GetTenantsQuery(), ct);
        return Ok(tenants);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TenantDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var tenant = await getTenantHandler.HandleAsync(new GetTenantQuery(id), ct);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateTenantResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateTenantCommand command, CancellationToken ct)
    {
        try
        {
            var result = await createHandler.HandleAsync(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.TenantId }, result);
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
            await updateHandler.HandleAsync(
                new UpdateTenantCommand(id, request.Name, request.IsActive, request.SubscriptionTier), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken ct)
    {
        var tenant = await getTenantHandler.HandleAsync(new GetTenantQuery(id), ct);
        if (tenant is null) return NotFound();

        await updateHandler.HandleAsync(
            new UpdateTenantCommand(id, tenant.Name, false, tenant.SubscriptionTier), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct)
    {
        var tenant = await getTenantHandler.HandleAsync(new GetTenantQuery(id), ct);
        if (tenant is null) return NotFound();

        await updateHandler.HandleAsync(
            new UpdateTenantCommand(id, tenant.Name, true, tenant.SubscriptionTier), ct);
        return NoContent();
    }
}

public sealed record UpdateTenantRequest(string Name, bool IsActive, SubscriptionTier SubscriptionTier);
