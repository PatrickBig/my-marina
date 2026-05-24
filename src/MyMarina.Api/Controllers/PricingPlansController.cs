using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.PricingPlans;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Authorize]
public class PricingPlansController(
    IQueryHandler<ListPricingPlansQuery, IReadOnlyList<PricingPlanDto>> listPlans,
    IQueryHandler<GetPricingPlanQuery, PricingPlanDto> getPlan,
    ICommandHandler<CreatePricingPlanCommand, PricingPlanDto> createPlan,
    ICommandHandler<UpdatePricingPlanCommand, PricingPlanDto> updatePlan,
    ICommandHandler<DeletePricingPlanCommand> deletePlan,
    ICommandHandler<SetDefaultPricingPlanCommand, PricingPlanDto> setDefault,
    ICommandHandler<BulkAssignPricingPlanCommand, BulkAssignResultDto> bulkAssign,
    IUserContext userContext)
    : ControllerBase
{
    // GET /marinas/{marinaId}/pricing/plans
    [HttpGet("marinas/{marinaId:guid}/pricing/plans")]
    [ProducesResponseType(typeof(IReadOnlyList<PricingPlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListPlans(Guid marinaId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();
        var plans = await listPlans.HandleAsync(new ListPricingPlansQuery(marinaId, userContext.UserId!.Value), ct);
        return Ok(plans);
    }

    // GET /marinas/{marinaId}/pricing/plans/{planId}
    [HttpGet("marinas/{marinaId:guid}/pricing/plans/{planId:guid}")]
    [ProducesResponseType(typeof(PricingPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlan(Guid marinaId, Guid planId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();
        try
        {
            var plan = await getPlan.HandleAsync(new GetPricingPlanQuery(planId, marinaId, userContext.UserId!.Value), ct);
            return Ok(plan);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // POST /marinas/{marinaId}/pricing/plans
    [HttpPost("marinas/{marinaId:guid}/pricing/plans")]
    [ProducesResponseType(typeof(PricingPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreatePlan(Guid marinaId, [FromBody] CreatePlanRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();
        var plan = await createPlan.HandleAsync(new CreatePricingPlanCommand(
            marinaId, userContext.UserId!.Value,
            request.Name, request.IsDefault,
            request.TransientRateKind, request.TransientAmount, request.TransientMinCharge,
            request.LeaseRateKind, request.LeaseAmount, request.LeaseMinCharge,
            request.AmenityAddOns ?? []), ct);
        return CreatedAtAction(nameof(GetPlan), new { marinaId, planId = plan.Id }, plan);
    }

    // PUT /marinas/{marinaId}/pricing/plans/{planId}
    [HttpPut("marinas/{marinaId:guid}/pricing/plans/{planId:guid}")]
    [ProducesResponseType(typeof(PricingPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlan(Guid marinaId, Guid planId, [FromBody] UpdatePlanRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();
        try
        {
            var plan = await updatePlan.HandleAsync(new UpdatePricingPlanCommand(
                planId, marinaId, userContext.UserId!.Value,
                request.Name,
                request.TransientRateKind, request.TransientAmount, request.TransientMinCharge,
                request.ClearTransientRate,
                request.LeaseRateKind, request.LeaseAmount, request.LeaseMinCharge,
                request.ClearLeaseRate,
                request.AmenityAddOns), ct);
            return Ok(plan);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // DELETE /marinas/{marinaId}/pricing/plans/{planId}
    [HttpDelete("marinas/{marinaId:guid}/pricing/plans/{planId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeletePlan(Guid marinaId, Guid planId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();
        try
        {
            await deletePlan.HandleAsync(new DeletePricingPlanCommand(planId, marinaId, userContext.UserId!.Value), ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    // POST /marinas/{marinaId}/pricing/plans/{planId}/set-default
    [HttpPost("marinas/{marinaId:guid}/pricing/plans/{planId:guid}/set-default")]
    [ProducesResponseType(typeof(PricingPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefault(Guid marinaId, Guid planId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();
        try
        {
            var plan = await setDefault.HandleAsync(new SetDefaultPricingPlanCommand(planId, marinaId, userContext.UserId!.Value), ct);
            return Ok(plan);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // POST /marinas/{marinaId}/pricing/plans/bulk-assign
    [HttpPost("marinas/{marinaId:guid}/pricing/plans/bulk-assign")]
    [ProducesResponseType(typeof(BulkAssignResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BulkAssign(Guid marinaId, [FromBody] BulkAssignRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();
        try
        {
            var result = await bulkAssign.HandleAsync(new BulkAssignPricingPlanCommand(
                marinaId, userContext.UserId!.Value,
                request.PricingPlanId,
                request.DockId,
                request.MinLength, request.MaxLength,
                request.MinBeam, request.MaxBeam,
                request.Amenities,
                request.CurrentPlanId,
                request.FilterUnassigned), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}

// ─── Request DTOs ─────────────────────────────────────────────────────────────

public sealed record CreatePlanRequest(
    string Name,
    bool IsDefault,
    RateKind? TransientRateKind,
    decimal? TransientAmount,
    decimal? TransientMinCharge,
    RateKind? LeaseRateKind,
    decimal? LeaseAmount,
    decimal? LeaseMinCharge,
    IReadOnlyList<AmenityAddOnDto>? AmenityAddOns
);

public sealed record UpdatePlanRequest(
    string? Name,
    RateKind? TransientRateKind,
    decimal? TransientAmount,
    decimal? TransientMinCharge,
    bool ClearTransientRate,
    RateKind? LeaseRateKind,
    decimal? LeaseAmount,
    decimal? LeaseMinCharge,
    bool ClearLeaseRate,
    IReadOnlyList<AmenityAddOnDto>? AmenityAddOns
);

public sealed record BulkAssignRequest(
    Guid PricingPlanId,
    Guid? DockId,
    decimal? MinLength,
    decimal? MaxLength,
    decimal? MinBeam,
    decimal? MaxBeam,
    IReadOnlyList<PlanAmenity>? Amenities,
    Guid? CurrentPlanId,
    bool FilterUnassigned = false
);
