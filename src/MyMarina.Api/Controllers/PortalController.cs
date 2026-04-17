using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Portal;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

/// <summary>
/// Customer self-service portal endpoints. All require the Customer role.
/// </summary>
[ApiController]
[Route("portal")]
[Authorize(Roles = "Customer")]
public class PortalController(
    IQueryHandler<GetPortalMeQuery, PortalMeDto?> meHandler,
    IQueryHandler<GetPortalSlipQuery, PortalSlipAssignmentDto?> slipHandler,
    IQueryHandler<GetPortalBoatsQuery, IReadOnlyList<PortalBoatDto>> boatsHandler,
    IQueryHandler<GetPortalInvoicesQuery, IReadOnlyList<PortalInvoiceDto>> invoicesHandler,
    IQueryHandler<GetPortalInvoiceQuery, PortalInvoiceDetailDto?> invoiceHandler,
    IQueryHandler<GetPortalMaintenanceRequestsQuery, IReadOnlyList<PortalMaintenanceRequestDto>> requestsHandler,
    ICommandHandler<SubmitMaintenanceRequestCommand, Guid> submitHandler,
    IQueryHandler<GetPortalAnnouncementsQuery, IReadOnlyList<PortalAnnouncementDto>> announcementsHandler
) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(PortalMeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var result = await meHandler.HandleAsync(new GetPortalMeQuery(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("slip")]
    [ProducesResponseType(typeof(PortalSlipAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetSlip(CancellationToken ct)
    {
        var result = await slipHandler.HandleAsync(new GetPortalSlipQuery(), ct);
        return result is null ? NoContent() : Ok(result);
    }

    [HttpGet("boats")]
    [ProducesResponseType(typeof(IReadOnlyList<PortalBoatDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBoats(CancellationToken ct)
        => Ok(await boatsHandler.HandleAsync(new GetPortalBoatsQuery(), ct));

    [HttpGet("invoices")]
    [ProducesResponseType(typeof(IReadOnlyList<PortalInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoices(CancellationToken ct)
        => Ok(await invoicesHandler.HandleAsync(new GetPortalInvoicesQuery(), ct));

    [HttpGet("invoices/{invoiceId:guid}")]
    [ProducesResponseType(typeof(PortalInvoiceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoice(Guid invoiceId, CancellationToken ct)
    {
        var result = await invoiceHandler.HandleAsync(new GetPortalInvoiceQuery(invoiceId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("maintenance-requests")]
    [ProducesResponseType(typeof(IReadOnlyList<PortalMaintenanceRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMaintenanceRequests(CancellationToken ct)
        => Ok(await requestsHandler.HandleAsync(new GetPortalMaintenanceRequestsQuery(), ct));

    [HttpPost("maintenance-requests")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitMaintenanceRequest(
        [FromBody] SubmitMaintenanceRequestCommand command,
        CancellationToken ct)
    {
        var id = await submitHandler.HandleAsync(command, ct);
        return StatusCode(StatusCodes.Status201Created, id);
    }

    [HttpGet("announcements")]
    [ProducesResponseType(typeof(IReadOnlyList<PortalAnnouncementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnnouncements(CancellationToken ct)
        => Ok(await announcementsHandler.HandleAsync(new GetPortalAnnouncementsQuery(), ct));
}
