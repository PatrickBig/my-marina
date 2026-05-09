using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("client-config")]
[AllowAnonymous]
public class ClientConfigController(IAuthenticationSchemeProvider schemeProvider) : ControllerBase
{
    private static readonly string[] SocialProviderSchemes = ["Google", "Apple", "Facebook"];

    // GET /client-config
    // Returns configuration the client needs before (or without) authentication.
    // Safe to call anonymously — contains no sensitive data.
    [HttpGet]
    [ProducesResponseType(typeof(ClientConfigDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get()
    {
        var registered = (await schemeProvider.GetAllSchemesAsync())
            .Select(s => s.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var socialProviders = SocialProviderSchemes
            .Where(registered.Contains)
            .Select(p => p.ToLowerInvariant())
            .ToArray();

        return Ok(new ClientConfigDto(socialProviders));
    }
}

public record ClientConfigDto(
    string[] SocialProviders
);
