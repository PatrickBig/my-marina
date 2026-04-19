using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MyMarina.Api.Infrastructure;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Demo;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Demo;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("demo")]
public class DemoController(
    ICommandHandler<ProvisionDemoTenantCommand, ProvisionDemoTenantResult> provisionHandler,
    ICommandHandler<CreateDemoSessionCommand, DemoSessionResult> sessionHandler,
    IOptions<DemoOptions> demoOptions) : ControllerBase
{
    /// <summary>
    /// Returns the capability list for the requested tier.
    /// Gated at Pro so callers need at least Pro to query Pro+ capabilities.
    /// Free capabilities are always public.
    /// </summary>
    [HttpGet("capabilities")]
    [Authorize]
    [RequiresTier(SubscriptionTier.Pro)]
    [ProducesResponseType(typeof(CapabilitiesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetCapabilities([FromQuery] string tier = "pro")
    {
        if (!Enum.TryParse<SubscriptionTier>(tier, ignoreCase: true, out var parsedTier))
            return BadRequest(new { message = $"Unknown tier: {tier}" });

        var capabilities = TierCapabilityRegistry.GetCapabilities(parsedTier);
        return Ok(new CapabilitiesResponse(parsedTier.ToString(), capabilities));
    }

    /// <summary>
    /// Returns free-tier capabilities without requiring authentication (for the public pricing table).
    /// </summary>
    [HttpGet("capabilities/public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AllTiersCapabilitiesResponse), StatusCodes.Status200OK)]
    public IActionResult GetPublicCapabilities()
    {
        var tiers = Enum.GetValues<SubscriptionTier>()
            .Select(t => new CapabilitiesResponse(t.ToString(), TierCapabilityRegistry.GetCapabilities(t)))
            .ToList();
        return Ok(new AllTiersCapabilitiesResponse(tiers));
    }

    /// <summary>
    /// Creates a per-visitor demo tenant seeded with full data and returns a short-lived JWT.
    /// No authentication required; rate-limited by IP.
    /// </summary>
    [HttpPost("session")]
    [AllowAnonymous]
    [EnableRateLimiting("demo-session")]
    [ProducesResponseType(typeof(DemoSessionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateSession(
        [FromQuery] string role = "operator",
        [FromQuery] string? tier = null,
        CancellationToken ct = default)
    {
        var resolvedTierStr = tier ?? demoOptions.Value.DefaultTier;
        if (!Enum.TryParse<SubscriptionTier>(resolvedTierStr, ignoreCase: true, out var parsedTier))
            return BadRequest(new { message = $"Unknown tier: {resolvedTierStr}" });

        var normalizedRole = role.ToLowerInvariant();
        if (normalizedRole is not ("operator" or "customer"))
            return BadRequest(new { message = $"Unknown role: {role}. Must be 'operator' or 'customer'." });

        var result = await sessionHandler.HandleAsync(
            new CreateDemoSessionCommand(normalizedRole, parsedTier), ct);

        return Ok(result);
    }
}

public sealed record CapabilitiesResponse(string Tier, IReadOnlyList<string> Capabilities);
public sealed record AllTiersCapabilitiesResponse(IReadOnlyList<CapabilitiesResponse> Tiers);
