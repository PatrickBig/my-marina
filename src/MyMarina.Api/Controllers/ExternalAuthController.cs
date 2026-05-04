using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;
using MyMarina.Infrastructure.Identity;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("auth/external")]
public class ExternalAuthController(
    ICommandHandler<ExternalLoginCommand, AuthResponse> externalLogin,
    ICommandHandler<LinkExternalProviderCommand> linkProvider,
    ICommandHandler<UnlinkExternalProviderCommand> unlinkProvider,
    IQueryHandler<GetLinkedProvidersQuery, IReadOnlyList<LinkedProviderDto>> getProviders,
    IUserContext userContext)
    : ControllerBase
{
    private static readonly HashSet<string> ValidProviders =
        new(StringComparer.OrdinalIgnoreCase) { "google", "apple", "facebook" };

    // GET /auth/external/{provider}?returnUrl=/
    // Initiates the OAuth challenge — browser is redirected to the provider.
    [HttpGet("{provider}")]
    public IActionResult Challenge(string provider, [FromQuery] string? returnUrl = "/")
    {
        if (!ValidProviders.Contains(provider))
            return BadRequest(new { error = "Unknown provider." });

        var callbackUrl = Url.Action(nameof(Callback), new { provider })!;
        var properties = new AuthenticationProperties
        {
            RedirectUri = callbackUrl,
            Items       = { ["returnUrl"] = returnUrl ?? "/" },
        };
        return Challenge(properties, NormalizeScheme(provider));
    }

    // GET /auth/external/{provider}/callback
    // Called by the OAuth middleware after a successful provider redirect.
    // Signs in or registers the user, then redirects to the SPA with tokens.
    [HttpGet("{provider}/callback")]
    public async Task<IActionResult> Callback(string provider, CancellationToken ct)
    {
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (!result.Succeeded)
            return Redirect("/login?error=external_auth_failed");

        var principal    = result.Principal!;
        var providerKey  = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var email        = principal.FindFirstValue(ClaimTypes.Email);
        var firstName    = principal.FindFirstValue(ClaimTypes.GivenName);
        var lastName     = principal.FindFirstValue(ClaimTypes.Surname);
        var ip           = HttpContext.Connection.RemoteIpAddress?.ToString();
        var returnUrl    = result.Properties?.Items.TryGetValue("returnUrl", out var ru) == true ? ru : "/";

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        try
        {
            var response = await externalLogin.HandleAsync(
                new ExternalLoginCommand(NormalizeScheme(provider), providerKey, email, firstName, lastName, ip), ct);

            return Redirect(BuildFrontendCallbackUrl(returnUrl!, response));
        }
        catch (ExternalEmailCollisionException)
        {
            return Redirect("/login?error=email_collision");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Redirect($"/login?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    // GET /auth/external/{provider}/link?returnUrl=/profile
    // [Authorize] — initiates an OAuth challenge to link a new provider to the signed-in account.
    [HttpGet("{provider}/link")]
    [Authorize]
    public IActionResult LinkChallenge(string provider, [FromQuery] string? returnUrl = "/profile")
    {
        if (!ValidProviders.Contains(provider))
            return BadRequest(new { error = "Unknown provider." });

        var callbackUrl = Url.Action(nameof(LinkCallback), new { provider })!;
        var properties = new AuthenticationProperties
        {
            RedirectUri = callbackUrl,
            Items       = { ["returnUrl"] = returnUrl ?? "/profile" },
        };
        return Challenge(properties, NormalizeScheme(provider));
    }

    // GET /auth/external/{provider}/link-callback
    // Called by the OAuth middleware after the link challenge completes.
    [HttpGet("{provider}/link-callback")]
    [Authorize]
    public async Task<IActionResult> LinkCallback(string provider, CancellationToken ct)
    {
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (!result.Succeeded)
            return Redirect("/profile?error=link_failed");

        var providerKey = result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var returnUrl   = result.Properties?.Items.TryGetValue("returnUrl", out var ru) == true ? ru : "/profile";
        var userId      = userContext.UserId!.Value;

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        try
        {
            await linkProvider.HandleAsync(
                new LinkExternalProviderCommand(userId, NormalizeScheme(provider), providerKey), ct);
            return Redirect($"{returnUrl}?linked={Uri.EscapeDataString(provider)}");
        }
        catch (Exception ex)
        {
            return Redirect($"{returnUrl}?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    // POST /auth/external/{provider}/unlink
    [HttpPost("{provider}/unlink")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Unlink(string provider, CancellationToken ct)
    {
        try
        {
            await unlinkProvider.HandleAsync(
                new UnlinkExternalProviderCommand(userContext.UserId!.Value, NormalizeScheme(provider)), ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // GET /auth/external/providers
    [HttpGet("providers")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<LinkedProviderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLinkedProviders(CancellationToken ct)
    {
        var providers = await getProviders.HandleAsync(
            new GetLinkedProvidersQuery(userContext.UserId!.Value), ct);
        return Ok(providers);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static string NormalizeScheme(string provider) => provider.ToLowerInvariant() switch
    {
        "google"   => "Google",
        "facebook" => "Facebook",
        "apple"    => "Apple",
        _          => provider,
    };

    private static string BuildFrontendCallbackUrl(string returnUrl, AuthResponse response)
    {
        var sep = returnUrl.Contains('?') ? '&' : '?';
        return $"{returnUrl}{sep}" +
               $"accessToken={Uri.EscapeDataString(response.AccessToken)}" +
               $"&refreshToken={Uri.EscapeDataString(response.RefreshToken)}" +
               $"&expiresAt={Uri.EscapeDataString(response.ExpiresAt.ToString("O"))}";
    }
}
