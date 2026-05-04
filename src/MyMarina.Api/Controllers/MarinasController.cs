using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Authorize]
public class MarinasController(
    ICommandHandler<CreateMarinaAccountCommand, MarinaSignupResponse> createAccount,
    ICommandHandler<UpdateMarinaCommand, MarinaDto> updateMarina,
    ICommandHandler<CreateDockCommand, DockDto> createDock,
    ICommandHandler<UpdateDockCommand, DockDto> updateDock,
    ICommandHandler<DeleteDockCommand> deleteDock,
    ICommandHandler<CreateSlipCommand, SlipDto> createSlip,
    ICommandHandler<UpdateSlipCommand, SlipDto> updateSlip,
    ICommandHandler<DeleteSlipCommand> deleteSlip,
    IQueryHandler<GetMarinaQuery, MarinaDto> getMarina,
    IQueryHandler<GetMyMarinasQuery, IReadOnlyList<MyMarinaDto>> getMyMarinas,
    IQueryHandler<GetDocksQuery, IReadOnlyList<DockDto>> getDocks,
    IQueryHandler<GetSlipsQuery, IReadOnlyList<SlipDto>> getSlips,
    IQueryHandler<GetSlipQuery, SlipDto> getSlip,
    IUserContext userContext)
    : ControllerBase
{
    // GET /marinas — list all marinas the current user has a relationship with
    [HttpGet("marinas")]
    [ProducesResponseType(typeof(IReadOnlyList<MyMarinaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyMarinas(CancellationToken ct)
    {
        var marinas = await getMyMarinas.HandleAsync(new GetMyMarinasQuery(), ct);
        return Ok(marinas);
    }

    // POST /marinas/signup — creates Tenant + Marina + Owner Membership, returns fresh JWT
    [HttpPost("marinas/signup")]
    [ProducesResponseType(typeof(MarinaSignupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Signup([FromBody] MarinaSignupRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<MarinaType>(request.MarinaType, ignoreCase: true, out var marinaType))
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["marinaType"] = [$"Unknown marina type: {request.MarinaType}"] }));

        var response = await createAccount.HandleAsync(new CreateMarinaAccountCommand(
            UserId: userContext.UserId!.Value,
            TenantName: request.TenantName,
            MarinaName: request.MarinaName,
            MarinaType: marinaType,
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString()), ct);

        return CreatedAtAction(nameof(GetMarina), new { marinaId = response.Marina.Id }, response);
    }

    // GET /marinas/{marinaId}
    [HttpGet("marinas/{marinaId:guid}")]
    [ProducesResponseType(typeof(MarinaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMarina(Guid marinaId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        try
        {
            var marina = await getMarina.HandleAsync(new GetMarinaQuery(marinaId, userContext.UserId!.Value), ct);
            return Ok(marina);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // PATCH /marinas/{marinaId}
    [HttpPatch("marinas/{marinaId:guid}")]
    [ProducesResponseType(typeof(MarinaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMarina(Guid marinaId, [FromBody] UpdateMarinaRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();
        try
        {
            var marina = await updateMarina.HandleAsync(new UpdateMarinaCommand(
                MarinaId: marinaId,
                RequestingUserId: userContext.UserId!.Value,
                Name: request.Name,
                AddressStreet: request.AddressStreet,
                AddressCity: request.AddressCity,
                AddressState: request.AddressState,
                AddressZip: request.AddressZip,
                AddressCountry: request.AddressCountry,
                Latitude: request.Latitude,
                Longitude: request.Longitude,
                PhoneNumber: request.PhoneNumber,
                Email: request.Email,
                Website: request.Website,
                Description: request.Description,
                TimeZoneId: request.TimeZoneId), ct);
            return Ok(marina);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ---------- Docks ----------

    [HttpGet("marinas/{marinaId:guid}/docks")]
    [ProducesResponseType(typeof(IReadOnlyList<DockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDocks(Guid marinaId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        var docks = await getDocks.HandleAsync(new GetDocksQuery(marinaId, userContext.UserId!.Value), ct);
        return Ok(docks);
    }

    [HttpPost("marinas/{marinaId:guid}/docks")]
    [ProducesResponseType(typeof(DockDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateDock(Guid marinaId, [FromBody] CreateDockRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();
        var dock = await createDock.HandleAsync(new CreateDockCommand(
            MarinaId: marinaId,
            RequestingUserId: userContext.UserId!.Value,
            Name: request.Name,
            Description: request.Description,
            SortOrder: request.SortOrder), ct);
        return CreatedAtAction(nameof(GetDocks), new { marinaId }, dock);
    }

    [HttpPatch("marinas/{marinaId:guid}/docks/{dockId:guid}")]
    [ProducesResponseType(typeof(DockDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDock(Guid marinaId, Guid dockId, [FromBody] UpdateDockRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();
        try
        {
            var dock = await updateDock.HandleAsync(new UpdateDockCommand(
                DockId: dockId,
                MarinaId: marinaId,
                RequestingUserId: userContext.UserId!.Value,
                Name: request.Name,
                Description: request.Description,
                SortOrder: request.SortOrder), ct);
            return Ok(dock);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("marinas/{marinaId:guid}/docks/{dockId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDock(Guid marinaId, Guid dockId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();
        try
        {
            await deleteDock.HandleAsync(new DeleteDockCommand(dockId, marinaId, userContext.UserId!.Value), ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ---------- Slips ----------

    [HttpGet("marinas/{marinaId:guid}/slips")]
    [ProducesResponseType(typeof(IReadOnlyList<SlipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSlips(Guid marinaId, [FromQuery] Guid? dockId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        var slips = await getSlips.HandleAsync(new GetSlipsQuery(marinaId, userContext.UserId!.Value, dockId), ct);
        return Ok(slips);
    }

    [HttpGet("marinas/{marinaId:guid}/slips/{slipId:guid}")]
    [ProducesResponseType(typeof(SlipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSlip(Guid marinaId, Guid slipId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();
        try
        {
            var slip = await getSlip.HandleAsync(new GetSlipQuery(slipId, marinaId, userContext.UserId!.Value), ct);
            return Ok(slip);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("marinas/{marinaId:guid}/slips")]
    [ProducesResponseType(typeof(SlipDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateSlip(Guid marinaId, [FromBody] CreateSlipRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();

        SlipType slipType = SlipType.Floating;
        if (request.SlipType is not null && !Enum.TryParse<SlipType>(request.SlipType, ignoreCase: true, out slipType))
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["slipType"] = [$"Unknown slip type: {request.SlipType}"] }));

        ElectricAmperage? electric = null;
        if (request.Electric.HasValue && !Enum.IsDefined(typeof(ElectricAmperage), request.Electric.Value))
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["electric"] = [$"Unknown amperage: {request.Electric}"] }));
        if (request.Electric.HasValue)
            electric = (ElectricAmperage)request.Electric.Value;

        var slip = await createSlip.HandleAsync(new CreateSlipCommand(
            MarinaId: marinaId,
            RequestingUserId: userContext.UserId!.Value,
            DockId: request.DockId,
            Name: request.Name,
            SlipType: slipType,
            MaxLength: request.MaxLength,
            MaxBeam: request.MaxBeam,
            MaxDraft: request.MaxDraft,
            HasElectric: request.HasElectric,
            Electric: electric,
            HasWater: request.HasWater,
            Notes: request.Notes), ct);

        return CreatedAtAction(nameof(GetSlip), new { marinaId, slipId = slip.Id }, slip);
    }

    [HttpPatch("marinas/{marinaId:guid}/slips/{slipId:guid}")]
    [ProducesResponseType(typeof(SlipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSlip(Guid marinaId, Guid slipId, [FromBody] UpdateSlipRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();

        SlipType? slipType = null;
        if (request.SlipType is not null)
        {
            if (!Enum.TryParse<SlipType>(request.SlipType, ignoreCase: true, out var parsed))
                return ValidationProblem(new ValidationProblemDetails(
                    new Dictionary<string, string[]> { ["slipType"] = [$"Unknown slip type: {request.SlipType}"] }));
            slipType = parsed;
        }

        SlipStatus? status = null;
        if (request.Status is not null)
        {
            if (!Enum.TryParse<SlipStatus>(request.Status, ignoreCase: true, out var parsed))
                return ValidationProblem(new ValidationProblemDetails(
                    new Dictionary<string, string[]> { ["status"] = [$"Unknown status: {request.Status}"] }));
            status = parsed;
        }

        ElectricAmperage? electric = null;
        if (request.Electric.HasValue)
            electric = (ElectricAmperage)request.Electric.Value;

        try
        {
            var slip = await updateSlip.HandleAsync(new UpdateSlipCommand(
                SlipId: slipId,
                MarinaId: marinaId,
                RequestingUserId: userContext.UserId!.Value,
                DockId: request.DockId,
                Name: request.Name,
                SlipType: slipType,
                MaxLength: request.MaxLength,
                MaxBeam: request.MaxBeam,
                MaxDraft: request.MaxDraft,
                HasElectric: request.HasElectric,
                Electric: electric,
                HasWater: request.HasWater,
                Status: status,
                DefaultTransientRateKind:  request.DefaultTransientRateKind,
                DefaultTransientBaseRate:  request.DefaultTransientBaseRate,
                DefaultTransientMinCharge: request.DefaultTransientMinCharge,
                ClearTransientRate:        request.ClearTransientRate,
                DefaultLeaseRateKind:      request.DefaultLeaseRateKind,
                DefaultLeaseBaseRate:      request.DefaultLeaseBaseRate,
                DefaultLeaseTerm:          request.DefaultLeaseTerm,
                ClearLeaseRate:            request.ClearLeaseRate,
                Notes: request.Notes), ct);
            return Ok(slip);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("marinas/{marinaId:guid}/slips/{slipId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSlip(Guid marinaId, Guid slipId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId, MembershipRole.Manager)) return Forbid();
        try
        {
            await deleteSlip.HandleAsync(new DeleteSlipCommand(slipId, marinaId, userContext.UserId!.Value), ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}

// ---------- request records ----------

public sealed record MarinaSignupRequest(string TenantName, string MarinaName, string MarinaType);
public sealed record UpdateMarinaRequest(
    string? Name,
    string? AddressStreet,
    string? AddressCity,
    string? AddressState,
    string? AddressZip,
    string? AddressCountry,
    decimal? Latitude,
    decimal? Longitude,
    string? PhoneNumber,
    string? Email,
    string? Website,
    string? Description,
    string? TimeZoneId
);
public sealed record CreateDockRequest(string Name, string? Description, int SortOrder);
public sealed record UpdateDockRequest(string? Name, string? Description, int? SortOrder);
public sealed record CreateSlipRequest(
    Guid? DockId,
    string Name,
    string? SlipType,
    decimal MaxLength,
    decimal MaxBeam,
    decimal MaxDraft,
    bool HasElectric,
    int? Electric,
    bool HasWater,
    string? Notes
);
public sealed record UpdateSlipRequest(
    Guid? DockId,
    string? Name,
    string? SlipType,
    decimal? MaxLength,
    decimal? MaxBeam,
    decimal? MaxDraft,
    bool? HasElectric,
    int? Electric,
    bool? HasWater,
    string? Status,
    // Transient default rate (set ClearTransientRate=true to remove)
    string? DefaultTransientRateKind,
    decimal? DefaultTransientBaseRate,
    decimal? DefaultTransientMinCharge,
    bool ClearTransientRate,
    // Lease default rate (set ClearLeaseRate=true to remove)
    string? DefaultLeaseRateKind,
    decimal? DefaultLeaseBaseRate,
    string? DefaultLeaseTerm,
    bool ClearLeaseRate,
    string? Notes
);
